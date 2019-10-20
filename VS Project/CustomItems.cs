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
                string json = File.ReadAllText(script.loadDir + @"\CustomItems\" + path);

                try
                {
                    CustomItem template = new CustomItem();
                    JsonUtility.FromJsonOverwrite(json, template);

                    Item target = ResourcesPrefabManager.Instance.GetItemPrefab(template.CloneTarget_ItemID);

                    ApplyCustomItem(template);

                    if (target is Weapon)
                    {
                        ApplyCustomWeapon(JsonUtility.FromJson<CustomWeapon>(json));
                    }
                    else if (target is Equipment)
                    {
                        ApplyCustomEquipment(JsonUtility.FromJson<CustomEquipment>(json));
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
            foreach (string path in Directory.GetFiles(script.loadDir + @"\CustomItems\Recipes"))
            {
                string json = File.ReadAllText(path);

                if (JsonUtility.FromJson<CustomRecipe>(json) is CustomRecipe template)
                {
                    DefineRecipe(template.Result_ItemID, template.CraftingType, template.Ingredient_ItemIDs);
                }
                yield return null;
            }

            script.Loading = false;
            SideLoader.Log("Loaded custom recipes", 0);
        }

        private void ApplyCustomItem(CustomItem template)
        {
            if (ResourcesPrefabManager.Instance.GetItemPrefab(template.CloneTarget_ItemID) is Item origItem)
            {
                SideLoader.Log("  - Applying template for " + template.Name);

                // clone it, set inactive, and dont destroy on load
                GameObject newItem = Instantiate(origItem.gameObject);
                newItem.SetActive(false);
                DontDestroyOnLoad(newItem);

                // get the Item component and set our custom ID first
                Item item = newItem.GetComponent<Item>();
                item.ItemID = template.New_ItemID;

                // set name and description
                string name = template.Name;
                string desc = template.Description;
                SetNameAndDesc(item, name, desc);

                // fix ResourcesPrefabManager dictionary
                if (At.GetValue(typeof(ResourcesPrefabManager), null, "ITEM_PREFABS") is Dictionary<string, Item> Items)
                {
                    Items.Add(item.ItemID.ToString(), item);
                    At.SetValue(Items, typeof(ResourcesPrefabManager), null, "ITEM_PREFABS");

                    SideLoader.Log(string.Format("Added {0} to RPM dictionary.", item.Name));
                }

                if (item.VisualPrefab != null)
                {
                    // clone the visual prefab so we can modify it without affecting the original item
                    Transform newVisuals = Instantiate(item.VisualPrefab);
                    newVisuals.gameObject.SetActive(false);
                    DontDestroyOnLoad(newVisuals);
                    item.VisualPrefab = newVisuals;

                    if (!string.IsNullOrEmpty(template.AssetBundle_Name) && !string.IsNullOrEmpty(template.VisualPrefabName) && script.LoadedBundles.ContainsKey(template.AssetBundle_Name))
                    {
                        foreach (AssetBundle bundle in script.LoadedBundles[template.AssetBundle_Name])
                        {
                            // check if this asset bundle contains our custom object
                            if (!(bundle.LoadAsset<GameObject>(template.VisualPrefabName) is GameObject customModel))
                            { continue; } // wrong assetbundle

                            // disable the original mesh first. 
                            foreach (Transform child in newVisuals)
                            {
                                // only the actual item visual will have both these components. will not disable particle fx or anything else.
                                if (child.GetComponent<BoxCollider>() && child.GetComponent<MeshRenderer>())
                                {
                                    child.gameObject.SetActive(false);
                                }
                            }

                            // set up our new model
                            GameObject newModel = Instantiate(customModel);
                            newModel.transform.parent = newVisuals.transform;

                            // fix rotation and pos
                            newModel.transform.position = template.Visual_PosOffset;
                            newModel.transform.rotation = Quaternion.Euler(template.Visual_RotOffset);
                        }
                    }

                    if (!string.IsNullOrEmpty(template.ItemIconName))
                    {
                        // set custom icon
                        Texture2D icon = script.TextureData[template.ItemIconName];
                        if (icon)
                        {
                            Sprite newIcon = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), Vector2.zero);
                            At.SetValue(newIcon, typeof(Item), item, "m_itemIcon");
                        }
                    }
                }

                // ========== set custom stats ==========

                if (item.GetComponent<ItemStats>() is ItemStats stats)
                {
                    stats.MaxDurability = template.Durability;
                    At.SetValue(template.BaseValue, typeof(ItemStats), stats, "m_baseValue"); // price  
                    At.SetValue(template.Weight, typeof(ItemStats), stats, "m_rawWeight");    // weight

                    item.SetStatScript(stats);
                }

                SideLoader.Log("initialized item " + template.Name, 0);
            }
            else
            {
                SideLoader.Log("::CustomItems - could not find CloneTarget_ItemID \"" + template.CloneTarget_ItemID + "\" for template " + template.Name, 0);
            }
        }

        private void ApplyCustomEquipment(CustomEquipment template)
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

        private void ApplyCustomWeapon(CustomWeapon template)
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

        private void SetNameAndDesc(Item item, string name, string desc)
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
    }

    public class CustomItem // TODO: IMPLEMENT THESE BASE/DERIVED CLASSES
    {
        // item ID
        public int New_ItemID;
        public int CloneTarget_ItemID;

        // asset bundle stuff
        public string AssetBundle_Name;
        public string VisualPrefabName;
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
