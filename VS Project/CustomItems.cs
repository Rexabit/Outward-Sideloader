using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;
using Localizer;
using System.IO;

namespace SideLoader
{
    public class CustomItems : MonoBehaviour
    {
        public SideLoader script;

        public IEnumerator LoadItems()
        {
            SideLoader.Log("Loading custom items...");

            foreach (string path in script.FilePaths[ResourceTypes.CustomItems])
            {
                string json = File.ReadAllText(path);

                try
                {
                    CustomItem template = new CustomItem();
                    JsonUtility.FromJsonOverwrite(json, template);

                    Item target = ResourcesPrefabManager.Instance.GetItemPrefab(template.CloneTarget_ItemID);

                    ApplyCustomItem(template);
                    script.LoadedCustomItems.Add(template.New_ItemID, ResourcesPrefabManager.Instance.GetItemPrefab(template.New_ItemID));

                    if (target is Weapon)
                    {
                        SetWeaponStats(JsonUtility.FromJson<CustomWeapon>(json));
                    }
                    else if (target is Equipment)
                    {
                        SetEquipmentStats(JsonUtility.FromJson<CustomEquipment>(json));
                    }
                }
                catch (Exception e)
                {
                    SideLoader.Log("Error applying custom json!\r\nError:" + e.Message + "\r\nStack Trace:" + e.StackTrace, 1);
                }

                yield return null;
            }

            SideLoader.Log("Loaded custom items", 0);

            // custom recipes
            foreach (string path in Directory.GetDirectories(script.loadDir))
            {
                if (!Directory.Exists(path + @"\CustomItems\Recipes"))
                {
                    continue;
                }

                foreach (string path2 in Directory.GetFiles(path + @"\CustomItems\Recipes"))
                {
                    string json = File.ReadAllText(path2);

                    if (JsonUtility.FromJson<CustomRecipe>(json) is CustomRecipe template)
                    {
                        DefineRecipe(template.Result_ItemID, template.CraftingType, template.Ingredient_ItemIDs);
                    }
                    yield return null;
                }
            }

            script.Loading = false;
            SideLoader.Log("Loaded custom recipes", 0);
        }

        public void ApplyCustomItem(CustomItem template)
        {
            if (ResourcesPrefabManager.Instance.GetItemPrefab(template.CloneTarget_ItemID) is Item origItem)
            {
                SideLoader.Log("  - Applying template for " + template.Name);

                // clone it, set inactive, and dont destroy on load
                Item item = CloneItem(origItem, template.New_ItemID);

                // set name and description
                string name = template.Name;
                string desc = template.Description;
                SetNameAndDesc(item, name, desc);

                // set item icon
                if (!string.IsNullOrEmpty(template.ItemIconName))
                {
                    Texture2D icon = script.TextureData[template.ItemIconName];
                    if (icon)
                    {
                        SetItemIcon(item, icon);
                    }
                }

                bool noVisualsFlag = false;
                bool noArmorVisualsFlag = false;

                // check if AssetBundle name is defined
                if (!string.IsNullOrEmpty(template.AssetBundle_Name) 
                    && script.LoadedBundles.ContainsKey(template.AssetBundle_Name)
                    && script.LoadedBundles[template.AssetBundle_Name] is AssetBundle bundle)
                {
                    // set normal visual prefab
                    if (!string.IsNullOrEmpty(template.VisualPrefabName) 
                        && bundle.LoadAsset<GameObject>(template.VisualPrefabName) is GameObject customModel)
                    {
                        Vector3 posoffset = template.Visual_PosOffset;
                        Vector3 rotoffset = template.Visual_RotOffset;

                        // setting armor "ground item" visuals, dont use the user's values here.
                        if (item is Armor)
                        {
                            posoffset = new Vector3(-1, -1, -1);
                            rotoffset = new Vector3(-1, -1, -1);
                        }

                        SetItemVisualPrefab(item, item.VisualPrefab, customModel.transform, posoffset, rotoffset);
                    }
                    else
                    {
                        // no visual prefab to set. clone the original and rename the material.

                        noVisualsFlag = true;
                    }

                    // set armor visual prefab
                    if (item is Armor)
                    {
                        if (!string.IsNullOrEmpty(template.ArmorVisualPrefabName) && bundle.LoadAsset<GameObject>(template.ArmorVisualPrefabName) is GameObject armorModel)
                        {
                            SetItemVisualPrefab(item,
                                item.SpecialVisualPrefabDefault,
                                armorModel.transform,
                                template.Visual_PosOffset,
                                template.Visual_RotOffset,
                                true,
                                template.HelmetHideFace,
                                template.HelmetHideHair);
                        }
                        else // no armor prefab to set
                        {
                            noArmorVisualsFlag = true;
                        }
                    }                        
                }
                else // no asset bundle.
                {
                    noVisualsFlag = true; noArmorVisualsFlag = true;
                }

                // if we should overwrite normal visual prefab, do that now
                if (noVisualsFlag)
                {
                    Transform newVisuals = Instantiate(item.VisualPrefab);
                    item.VisualPrefab = newVisuals;
                    DontDestroyOnLoad(newVisuals);

                    foreach (Transform child in newVisuals)
                    {
                        if (child.GetComponent<BoxCollider>() && child.GetComponent<MeshRenderer>() is MeshRenderer mesh)
                        {
                            string newMatName = "tex_itm_" + template.New_ItemID + "_" + template.Name;

                            OverwriteMaterials(mesh.material, newMatName);
                        }
                    }
                }

                // if we should overwrite armor visuals, do that now
                if (noArmorVisualsFlag)
                {
                    Transform newArmorVisuals = Instantiate(item.SpecialVisualPrefabDefault);
                    item.SpecialVisualPrefabDefault = newArmorVisuals;
                    DontDestroyOnLoad(newArmorVisuals);

                    if (newArmorVisuals.GetComponent<SkinnedMeshRenderer>() is SkinnedMeshRenderer mesh)
                    {
                        string newMatName = "tex_cha_" + template.New_ItemID + "_" + template.Name;

                        OverwriteMaterials(mesh.material, newMatName);
                    }
                }

                // ========== set custom stats ==========
                SetBaseItemStats(item, template.Durability, template.BaseValue, template.Weight);                

                SideLoader.Log("initialized item " + template.Name, 0);
            }
            else
            {
                SideLoader.Log("::CustomItems - could not find CloneTarget_ItemID \"" + template.CloneTarget_ItemID + "\" for template " + template.Name, 0);
            }
        }

        public Item CloneItem(Item origItem, int newID)
        {
            // clone it, set inactive, and dont destroy on load
            GameObject newItem = Instantiate(origItem.gameObject);
            newItem.SetActive(false);
            DontDestroyOnLoad(newItem);

            // get the Item component and set our custom ID first
            Item item = newItem.GetComponent<Item>();
            item.ItemID = newID;

            // fix ResourcesPrefabManager dictionary
            if (At.GetValue(typeof(ResourcesPrefabManager), null, "ITEM_PREFABS") is Dictionary<string, Item> Items)
            {
                if (Items.ContainsKey(item.ItemID.ToString()))
                {
                    Items[item.ItemID.ToString()] = item;
                }
                else
                {
                    Items.Add(item.ItemID.ToString(), item);
                }

                At.SetValue(Items, typeof(ResourcesPrefabManager), null, "ITEM_PREFABS");

                //SideLoader.Log(string.Format("Added {0} to RPM dictionary.", item.Name));
            }

            return item;
        }

        public void SetNameAndDesc(Item item, string name, string desc)
        {
            ItemLocalization loc = new ItemLocalization(name, desc);

            At.SetValue(name, typeof(Item), item, "m_name");

            if (At.GetValue(typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalization") is Dictionary<int, ItemLocalization> dict)
            {
                if (dict.ContainsKey(item.ItemID))
                {
                    dict[item.ItemID] = loc;
                }
                else
                {
                    dict.Add(item.ItemID, loc);
                }
                At.SetValue(dict, typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalization");
            }
        }

        public void SetItemIcon(Item item, Texture2D icon)
        {
            Sprite sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), Vector2.zero);
            At.SetValue(sprite, typeof(Item), item, "m_itemIcon");
        }

        public void SetItemVisualPrefab(Item item, Transform origVisuals, Transform newVisuals, Vector3 position_offset, Vector3 rotation_offset, bool SetSpecialPrefab = false, bool HelmetHideFace = false, bool HelmetHideHair = false)
        {
            // clone the visual prefab so we can modify it without affecting the original item
            Transform clone = Instantiate(origVisuals);

            clone.gameObject.SetActive(false);
            DontDestroyOnLoad(clone);
            
            if (SetSpecialPrefab)
            {
                item.SpecialVisualPrefabDefault = clone;
            }
            else
            {
                item.VisualPrefab = clone;
            }

            Vector3 origPos = Vector3.zero;
            Vector3 origRot = Vector3.zero;

            // set up our new model
            GameObject newModel = Instantiate(newVisuals.gameObject);
            newModel.transform.parent = clone.transform;

            // if we're setting an Armor Special (worn armor) prefab, handle that logic
            if (item is Armor && SetSpecialPrefab)
            {
                origPos = origVisuals.transform.position;
                origRot = origVisuals.transform.rotation.eulerAngles;

                ArmorVisuals visuals = newModel.AddComponent<ArmorVisuals>();

                if ((item as Armor).EquipSlot == EquipmentSlot.EquipmentSlotIDs.Helmet)
                {
                    visuals.HideFace = HelmetHideFace;
                    visuals.HideHair = HelmetHideHair;
                }

                foreach (MeshRenderer mesh in newModel.GetComponents<MeshRenderer>())
                {
                    DestroyImmediate(mesh);
                }
            }
            else // setting a normal prefab. disable the original mesh first.
            { 
                foreach (Transform child in clone)
                {
                    // only the actual item visuals will have both of these components. this will not disable particle fx or anything else.
                    if (child.GetComponent<BoxCollider>() && child.GetComponent<MeshRenderer>())
                    {
                        origPos = child.transform.position;
                        origRot = child.transform.rotation.eulerAngles;

                        child.gameObject.SetActive(false);

                        break;
                    }
                }
            }

            // position / rotation stuff

            // if user set it to -1-1-1, just use orig values

            if (position_offset == new Vector3(-1, -1, -1)) { newModel.transform.position = origPos; }
            else { newModel.transform.position = position_offset; }

            if (rotation_offset == new Vector3(-1, -1, -1)) { newModel.transform.rotation = Quaternion.Euler(origRot); }
            else { newModel.transform.rotation = Quaternion.Euler(rotation_offset); }
        }

        private void OverwriteMaterials(Material material, string newName)
        {
            Texture newMainTex = Instantiate(material.mainTexture);
            material.mainTexture = newMainTex;
            DontDestroyOnLoad(newMainTex);

            // set mainTexture name (_d)
            newMainTex.name = newName + "_d";

            // check each shader material suffix name
            foreach (KeyValuePair<string, string> entry in TexReplacer.TextureSuffixes)
            {
                if (entry.Key == "_m" || entry.Key == "_i") { continue; }

                if (material.GetTexture(entry.Value) is Texture tex)
                {
                    Texture newTex = Instantiate(tex);
                    DontDestroyOnLoad(newTex);

                    material.SetTexture(entry.Key, newTex);

                    newTex.name = newName + entry.Key;
                }
            }
        }

        public void SetBaseItemStats(Item item, int maxDurability, int baseValue, float weight)
        {
            if (item.GetComponent<ItemStats>() is ItemStats stats)
            {
                stats.MaxDurability = maxDurability;
                At.SetValue(baseValue, typeof(ItemStats), stats, "m_baseValue"); // price  
                At.SetValue(weight, typeof(ItemStats), stats, "m_rawWeight");    // weight

                item.SetStatScript(stats);
            }
        }


        // these two require a derived type of CustomItem, but they only need the NEW item ID and the relevant fields for Equipment / Weapon stuff

        public void SetEquipmentStats(CustomEquipment template)
        {
            if (ResourcesPrefabManager.Instance.GetItemPrefab(template.New_ItemID) is Item item)
            {
                EquipmentStats stats = (item as Equipment).Stats;

                At.SetValue(template.DamageBonuses, typeof(EquipmentStats), stats, "m_damageAttack");
                At.SetValue(template.DamageResistances, typeof(EquipmentStats), stats, "m_damageResistance");
                float[] physProt = new float[9] { template.PhysProtection, 0, 0, 0, 0, 0, 0, 0, 0 };
                At.SetValue(physProt, typeof(EquipmentStats), stats, "m_damageProtection");
                At.SetValue(template.ImpactResistance, typeof(EquipmentStats), stats, "m_impactResistance");
                At.SetValue(template.ManaUseModifier, typeof(EquipmentStats), stats, "m_manaUseModifier");
                At.SetValue(template.HealthBonus, typeof(EquipmentStats), stats, "m_maxHealthBonus");
                At.SetValue(template.MovementPenalty, typeof(EquipmentStats), stats, "m_movementPenalty");
                At.SetValue(template.PouchBonus, typeof(EquipmentStats), stats, "m_pouchCapacityBonus");
                At.SetValue(template.StaminaUsePenalty, typeof(EquipmentStats), stats, "m_staminaUsePenalty");
                At.SetValue(template.ColdProtection, typeof(EquipmentStats), stats, "m_coldProtection");
                At.SetValue(template.HeatProtection, typeof(EquipmentStats), stats, "m_heatProtection");
            }
        }

        public void SetWeaponStats(CustomWeapon template)
        {
            if (ResourcesPrefabManager.Instance.GetItemPrefab(template.New_ItemID) is Item item)
            {
                WeaponStats stats = (item as Weapon).Stats;

                // equipment stats (which weapons can actually use and display)
                At.SetValue(template.DamageBonuses, typeof(EquipmentStats), stats as EquipmentStats, "m_damageAttack");
                At.SetValue(template.ManaUseModifier, typeof(EquipmentStats), stats as EquipmentStats, "m_manaUseModifier");

                // set base weapon stats
                stats.BaseDamage = template.BaseDamage;
                stats.Impact = template.Impact;
                stats.AttackSpeed = template.AttackSpeed;

                // set attack steps
                for (int i = 0; i < stats.Attacks.Count(); i++)
                {
                    var step = stats.Attacks[i];

                    List<float> newDamage = new List<float>();
                    foreach (DamageType type in template.BaseDamage.List)
                    {
                        newDamage.Add(type.Damage);
                    }
                    float newImpact = template.Impact;
                    StatHelpers.SetScaledDamages((item as Weapon).Type, i, ref newDamage, ref newImpact);

                    step.Damage = newDamage;
                    step.Knockback = newImpact;
                }

                // =========== hit effects =============

                // remove existing hit effects
                if (item.transform.Find("HitEffects") is Transform t)
                {
                    t.transform.parent = null;
                    DestroyImmediate(t.gameObject);
                }
                // make new hit effects
                GameObject hiteffects = new GameObject("HitEffects");
                hiteffects.transform.parent = item.transform;
                foreach (string effect in template.hitEffects)
                {
                    if (ResourcesPrefabManager.Instance.GetStatusEffectPrefab(effect) is StatusEffect status)
                    {
                        hiteffects.AddComponent(new AddStatusEffectBuildUp
                        {
                            Status = status,
                            BuildUpValue = 60.0f,
                        });
                    }
                }
            }
        }
      


        // custom recipes

        public static void DefineRecipe(int ItemID, int craftingType, List<int> IngredientIDs)
        {
            if (ResourcesPrefabManager.Instance.GetItemPrefab(ItemID) is Item item)
            {
                SideLoader.Log("  - Defining recipe for " + item.Name);

                Recipe recipe = Recipe.CreateInstance("Recipe") as Recipe;
                recipe.SetCraftingType((Recipe.CraftingType)craftingType);
                recipe.SetRecipeID(ItemID);
                recipe.SetRecipeName(item.Name);
                recipe.SetRecipeResults(item, 1);

                RecipeIngredient[] ingredients = new RecipeIngredient[IngredientIDs.Count()];
                for (int i = 0; i < IngredientIDs.Count(); i++)
                {
                    int id = IngredientIDs[i];

                    RecipeIngredient ingredient = new RecipeIngredient()
                    {
                        ActionType = RecipeIngredient.ActionTypes.AddSpecificIngredient,
                        AddedIngredient = ResourcesPrefabManager.Instance.GetItemPrefab(id),
                    };
                    ingredients[i] = ingredient;
                }
                recipe.SetRecipeIngredients(ingredients);
                recipe.Init();

                // add to recipe dictionary
                if (At.GetValue(typeof(RecipeManager), RecipeManager.Instance, "m_recipes") is Dictionary<string, Recipe> dict
                    && At.GetValue(typeof(RecipeManager), RecipeManager.Instance, "m_recipeUIDsPerUstensils") is Dictionary<Recipe.CraftingType, List<UID>> dict2)
                {
                    // add to main recipe dictionary
                    dict.Add(recipe.UID, recipe);
                    At.SetValue(dict, typeof(RecipeManager), RecipeManager.Instance, "m_recipes");

                    // add to the "UID per Utensil" dictionary thing
                    if (!dict2.ContainsKey(recipe.CraftingStationType))
                    {
                        dict2.Add(recipe.CraftingStationType, new List<UID>());
                    }
                    dict2[recipe.CraftingStationType].Add(recipe.UID);
                    At.SetValue(dict2, typeof(RecipeManager), RecipeManager.Instance, "m_recipeUIDsPerUstensils");

                    SideLoader.Log("added " + item.Name + " to custom recipe to dict");
                }
            }
        }

        // SetupSkill is an example of how to make a custom Passive skill with AffectStat components. leaving for now.

        //private GameObject SetupSkill()
        //{
        //    try
        //    {
        //        // create a new GameObject
        //        GameObject mySkill = new GameObject("TestPassiveSkill");
        //        DontDestroyOnLoad(mySkill);
        //        mySkill.SetActive(false);

        //        // basic item initialization (bare minimum)
        //        PassiveSkill skillComponent = mySkill.AddComponent(new PassiveSkill()
        //        {
        //            ItemID = 9996666,
        //        });
        //        SL.Instance.CustomItems.SetNameAndDesc(skillComponent as Item, "TestSkill", "Test.");

        //        // create the Effects child
        //        GameObject fxChild = new GameObject("Effects");
        //        fxChild.transform.parent = mySkill.transform;

        //        // AffectStat basic init
        //        AffectStat affectStat = fxChild.AddComponent(new AffectStat()
        //        {
        //            AffectedStat = new TagSourceSelector(TagSourceManager.Instance.GetTag("96")), // trial and error to find these
        //            Value = 50,
        //            Duration = -1,
        //            Tags = new TagSourceSelector[0],
        //            RequireRegistration = false,
        //        });

        //        // set EffectFamily Type. This is important. See "EffectType.Families" static dictionary
        //        At.SetValue(new EffectTypeSelector() { SelectedEffectTypeName = "None" }, typeof(Effect), affectStat, "m_effectType");

        //        // create the list of Effect comps to link up to the actual PassiveSkill. Each "Effect" class component goes on this list.
        //        List<Effect> effects = new List<Effect>() { affectStat };

        //        // set the list of Effects to the skill so they are actually registered
        //        At.SetValue(effects, typeof(PassiveSkill), skillComponent, "m_passiveEffects");

        //        return mySkill;
        //    }
        //    catch (Exception e)
        //    {
        //        SideLoader.Log(string.Format("Error with SetupSkill! Error: {0}, Stack trace: {1}", e.Message, e.StackTrace), 1);

        //        return null;
        //    }
        //}
    }

    public class CustomItem
    {
        // item ID
        public int New_ItemID;
        public int CloneTarget_ItemID;

        // asset bundle stuff
        public string AssetBundle_Name;
        public string VisualPrefabName;
        public string ArmorVisualPrefabName;
        public bool HelmetHideFace;
        public bool HelmetHideHair;
        public string ItemIconName;

        // visual prefab custom alignment
        public Vector3 Visual_PosOffset;
        public Vector3 Visual_RotOffset;

        // actual item stuff
        public string Name;
        public string Description;

        // base itemstats
        public int Durability;
        public int BaseValue;
        public float Weight;
    }

    public class CustomEquipment : CustomItem
    {
        // EquipmentStats
        public float[] DamageBonuses = new float[9];
        public float[] DamageResistances = new float[9];
        public float PhysProtection;
        public float ImpactResistance;
        public float ManaUseModifier;
        public float StaminaUsePenalty;
        public float MovementPenalty;
        public float HealthBonus;
        public float PouchBonus;
        public float ColdProtection;
        public float HeatProtection;
    }

    public class CustomWeapon : CustomEquipment
    {
        // WeaponStats
        public float AttackSpeed;               
        public float Impact;
        public DamageList BaseDamage = new DamageList(
            new DamageType[]
            {
                new DamageType(DamageType.Types.Physical, 0),
                new DamageType(DamageType.Types.Ethereal, 0),
                new DamageType(DamageType.Types.Decay, 0),
                new DamageType(DamageType.Types.Electric, 0),
                new DamageType(DamageType.Types.Frost, 0),
                new DamageType(DamageType.Types.Fire, 0),
            }
        );

        // add status effect buildups
        public List<string> hitEffects = new List<string>() { "EffectName1", "EffectName2" };
    }

    public class CustomRecipe
    {
        public int Result_ItemID;
        public int CraftingType; // 0: Alchemy, 1: Cooking, 2: Surival
        public List<int> Ingredient_ItemIDs = new List<int>() { 0000000, 0000000, 0000000, 0000000 };
    }
}
