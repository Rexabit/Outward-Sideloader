using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;
using Localizer;
using System.IO;
//using OModAPI;

namespace SideLoader
{
    public class CustomItems : MonoBehaviour
    {
        public static CustomItems Instance;

        internal void Awake()
        {
            Instance = this;
        }

        public IEnumerator LoadItems()
        {
            SideLoader.Log("Loading custom items...");

            foreach (string path in SL.Instance.FilePaths[ResourceTypes.CustomItems])
            {
                string json = File.ReadAllText(path);

                try
                {
                    CustomItem template = new CustomItem();
                    JsonUtility.FromJsonOverwrite(json, template);

                    Item target = ResourcesPrefabManager.Instance.GetItemPrefab(template.CloneTarget_ItemID);

                    ApplyCustomItem(template);
                    SL.Instance.LoadedCustomItems.Add(template.New_ItemID, ResourcesPrefabManager.Instance.GetItemPrefab(template.New_ItemID));

                    if (target is Weapon)
                    {
                        SetWeaponStats(JsonUtility.FromJson<CustomWeapon>(json));
                    }
                    else if (target is Equipment)
                    {
                        SetEquipmentStats(JsonUtility.FromJson<CustomEquipment>(json));
                    }

                    if (target is Skill)
                    {
                        SetSkillStats(JsonUtility.FromJson<CustomSkill>(json));
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
            foreach (string path in Directory.GetDirectories(SL.Instance.loadDir))
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

            SL.Instance.Loading = false;
            SideLoader.Log("Loaded custom recipes", 0);
        }


        // ============ Class Setups ============== //

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
                if (!string.IsNullOrEmpty(template.ItemIconName) && SL.Instance.TextureData.ContainsKey(template.ItemIconName))
                {
                    Texture2D icon = SL.Instance.TextureData[template.ItemIconName];
                    if (icon)
                    {
                        SetItemIcon(item, icon);
                    }
                }

                bool noVisualsFlag = false;
                bool noArmorVisualsFlag = false;

                // check if AssetBundle name is defined
                if (!string.IsNullOrEmpty(template.AssetBundle_Name) 
                    && SL.Instance.LoadedBundles.ContainsKey(template.AssetBundle_Name)
                    && SL.Instance.LoadedBundles[template.AssetBundle_Name] is AssetBundle bundle)
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
                if (noVisualsFlag && item.VisualPrefab != null)
                {
                    bool customDefined = false;

                    foreach (string suffix in TexReplacer.TextureSuffixes.Keys)
                    {
                        string search = "tex_itm_" + template.New_ItemID + "_" + template.Name + suffix;
                        if (SL.Instance.TextureData.ContainsKey(search))
                        {
                            customDefined = true;
                            break;
                        }
                    }

                    if (customDefined)
                    {
                        Transform newVisuals = Instantiate(item.VisualPrefab);
                        item.VisualPrefab = newVisuals;
                        DontDestroyOnLoad(newVisuals);

                        foreach (Transform child in newVisuals)
                        {
                            if (child.GetComponent<BoxCollider>() && child.GetComponent<MeshRenderer>() is MeshRenderer mesh)
                            {
                                SideLoader.Log("Overwriting ItemVisuals for " + item.Name);

                                string newMatName = "tex_itm_" + template.New_ItemID + "_" + template.Name;

                                Material m = Instantiate(mesh.material);
                                DontDestroyOnLoad(m);

                                mesh.material = m;
                                OverwriteMaterials(m, newMatName);
                            }
                            else { continue; }
                            break;
                        }
                    }
                    else
                    {
                        SideLoader.Log("No custom ItemVisuals defined for " + item.Name);
                    }
                }

                // if we should overwrite armor visuals, do that now
                if (noArmorVisualsFlag && item.SpecialVisualPrefab != null)
                {
                    bool customDefined = false;

                    foreach (string suffix in TexReplacer.TextureSuffixes.Keys)
                    {
                        if (SL.Instance.TextureData.ContainsKey("tex_cha_" + template.New_ItemID + "_" + template.Name + suffix))
                        {
                            customDefined = true;
                            break;
                        }
                    }

                    if (customDefined)
                    {
                        Transform newArmorVisuals = Instantiate(item.SpecialVisualPrefabDefault);
                        item.SpecialVisualPrefabDefault = newArmorVisuals;
                        DontDestroyOnLoad(newArmorVisuals);

                        if (newArmorVisuals.GetComponent<SkinnedMeshRenderer>() is SkinnedMeshRenderer mesh)
                        {
                            SideLoader.Log("Overwriting SpecialVisualPrefab visuals for " + item.Name);

                            string newMatName = "tex_cha_" + template.New_ItemID + "_" + template.Name;

                            OverwriteMaterials(mesh.material, newMatName);
                        }
                    }
                    else
                    {
                        SideLoader.Log("No custom ArmorVisuals defined for " + item.Name);
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
            origItem.gameObject.SetActive(false);
            GameObject newItem = Instantiate(origItem.gameObject);
            origItem.gameObject.SetActive(true);

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
            var sprite = TexReplacer.CreateSprite(icon);            
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
            newMainTex.name = newName + "_d";

            // check each shader material suffix name
            foreach (KeyValuePair<string, string> entry in TexReplacer.TextureSuffixes)
            {
                if (entry.Key == "_d") { continue; } // already set MainTexture

                if (material.GetTexture(entry.Value) is Texture tex)
                {
                    Texture newTex = Instantiate(tex);
                    DontDestroyOnLoad(newTex);

                    material.SetTexture(entry.Value, newTex);

                    newTex.name = newName + entry.Key;
                }
            }
        }

        public void SetBaseItemStats(Item item, int maxDurability, int baseValue, float weight)
        {
            if (item.GetComponent<ItemStats>() is ItemStats stats)
            {
                stats.MaxDurability = maxDurability;
                At.SetValue(baseValue, typeof(ItemStats), stats, "mSL.InstanceValue"); // price  
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
      
        // set fields for skills

        public void SetSkillStats(CustomSkill template)
        {
            if (ResourcesPrefabManager.Instance.GetItemPrefab(template.New_ItemID) is Skill skill)
            {
                skill.Cooldown = template.Cooldown;
                skill.ManaCost = template.ManaCost;
                skill.StaminaCost = template.StaminaCost;

                // set skill tree icon
                if (!string.IsNullOrEmpty(template.SkillTreeIconName) && SL.Instance.TextureData.ContainsKey(template.SkillTreeIconName))
                {
                    CustomSkills.SetSkillSmallIcon(template.New_ItemID, template.SkillTreeIconName);
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

    public class CustomSkill : CustomItem
    {
        public string SkillTreeIconName;
        public float Cooldown;
        public float StaminaCost;
        public float ManaCost;
    }

    public class CustomRecipe
    {
        public int Result_ItemID;
        public int CraftingType; // 0: Alchemy, 1: Cooking, 2: Surival
        public List<int> Ingredient_ItemIDs = new List<int>() { 0000000, 0000000, 0000000, 0000000 };
    }
}
