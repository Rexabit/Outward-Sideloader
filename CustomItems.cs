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

        private bool Loading = false;

        public IEnumerator LoadItems()
        {
            script.Log("Loading custom items...");

            // custom weapons
            Loading = true;
            StartCoroutine(LoadWeapons());
            while (Loading) { yield return null; }

            script.Log("Loaded custom items", 0);
        }

        private IEnumerator LoadWeapons()
        {
            List<CustomWeapon> WeaponsToAdd = new List<CustomWeapon>();

            // load files
            foreach (string path in script.FilePaths[ResourceTypes.CustomItems])
            {
                if (JsonUtility.FromJson<CustomWeapon>(File.ReadAllText(script.loadDir + @"\CustomItems\" + path)) is CustomWeapon template)
                {
                    WeaponsToAdd.Add(template);
                }
            }

            // apply templates
            foreach (CustomWeapon template in WeaponsToAdd)
            {
                ApplyCustomWeapon(template);
            }

            Loading = false;
            yield return null;
        }

        private void ApplyCustomWeapon(CustomWeapon template)
        {
            if (ResourcesPrefabManager.Instance.GetItemPrefab(template.CloneTarget_ItemID) is Weapon origWeapon)
            {
                // clone it, set inactive, and dont destroy on load
                GameObject newSword = Instantiate(origWeapon.gameObject);
                newSword.SetActive(false);
                DontDestroyOnLoad(newSword);

                // get the Item component and set our custom ID first
                Item item = newSword.GetComponent<Item>();
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

                    script.Log(string.Format("Added {0} to RPM dictionary.", item.Name));
                }

                // clone the visual prefab so we can modify it without affecting the original item
                Transform newVisuals = Instantiate(item.VisualPrefab);
                newVisuals.gameObject.SetActive(false);
                DontDestroyOnLoad(newVisuals);
                item.VisualPrefab = newVisuals;

                if (!string.IsNullOrEmpty(template.AssetBundle_Name) && !string.IsNullOrEmpty(template.VisualPrefabName) && script.LoadedBundles.ContainsKey(template.AssetBundle_Name))
                {
                    foreach (AssetBundle bundle in script.LoadedBundles[template.AssetBundle_Name])
                    {
                        // check if this asset bundle contains our custom Sphere object
                        if (!(bundle.LoadAsset<GameObject>(template.VisualPrefabName) is GameObject customModel))
                        { continue; } // wrong assetbundle

                        // disable the original mesh first. 
                        foreach (Transform child in newVisuals)
                        {
                            if (child.GetComponent<BoxCollider>() && child.GetComponent<MeshRenderer>())
                            {
                                child.gameObject.SetActive(false);
                            }
                        }

                        // set up our new model
                        GameObject newModel = Instantiate(customModel);
                        newModel.transform.parent = newVisuals.transform;
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

                // ========== set custom stats ==========

                WeaponStats stats = item.GetComponent<ItemStats>() as WeaponStats;

                stats.MaxDurability = template.Durability;
                At.SetValue(template.BaseValue, typeof(ItemStats), stats as ItemStats, "m_baseValue"); // price  
                At.SetValue(template.Weight, typeof(ItemStats), stats as ItemStats, "m_rawWeight");    // weight

                // equipment stats
                At.SetValue(template.DamageBonuses, typeof(EquipmentStats), stats as EquipmentStats, "m_damageAttack");
                At.SetValue(template.ManaUseModifier, typeof(EquipmentStats), stats as EquipmentStats, "m_manaUseModifier");

                // calculate damage and impact ratios for AttackData steps first
                float dmgRatio = (float)((decimal)template.BaseDamage.TotalDamage / (decimal)stats.BaseDamage.TotalDamage);
                float impRatio = (float)((decimal)template.Impact / (decimal)stats.Impact);

                // set base weapon stats
                stats.BaseDamage = template.BaseDamage;
                stats.Impact = template.Impact;
                stats.AttackSpeed = template.AttackSpeed;

                // set attack steps
                for (int i = 0; i < stats.Attacks.Count(); i++)
                {
                    var step = stats.Attacks[i];
                    for (int j = 0; j < step.Damage.Count; j++)
                    {
                        step.Damage[j] *= dmgRatio;
                    }
                    step.Knockback *= impRatio;
                }

                // apply stat script
                item.SetStatScript(stats);

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

                // ========== custom recipe ============

                if (template.RecipeIngredientIDs != null && template.RecipeIngredientIDs.Count() > 0)
                {
                    Recipe recipe = new Recipe();
                    recipe.SetCraftingType(Recipe.CraftingType.Survival);
                    recipe.SetRecipeID(template.New_ItemID);
                    recipe.SetRecipeName(template.Name);
                    recipe.SetRecipeResults(item, 1);

                    RecipeIngredient[] ingredients = new RecipeIngredient[template.RecipeIngredientIDs.Count()];
                    for (int i = 0; i < template.RecipeIngredientIDs.Count(); i++)
                    {
                        int id = template.RecipeIngredientIDs[i];

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

                        script.Log("added " + template.Name + " to custom recipe to dict");
                    }
                }

                script.Log("initialized item " + template.Name, 0);
            }
            else
            {
                script.Log("::CustomItems - could not find CloneTarget_ItemID \"" + template.CloneTarget_ItemID + " for template " + template.Name, 0);
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

        //public static readonly CustomWeapon DarkBrand = new CustomWeapon
        //{
        //    New_ItemID = 6666666,
        //    CloneTarget_ItemID = 2000151, // strange rusted sword
        //    AssetBundle_Name = "mybundle",
        //    VisualPrefabName = "darkbrand visuals",
        //    ItemIconName = "DarkBrandIcon",
        //    Name = "Blight",
        //    Description = "The Commander said to make this sword fit for the Scourge.\n\nInflicts Curse on enemies.",
        //    Durability = 1000,
        //    BaseValue = 1000,
        //    Weight = 3,
        //    DamageBonuses = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        //    ManaUseModifier = 0,
        //    AttackSpeed = 1.2f,
        //    Impact = 25,
        //    BaseDamage = new DamageList(new DamageType(DamageType.Types.Physical, 36)),
        //    hitEffects = new List<string> { "Curse" },
        //    RecipeIngredientIDs = new int[] { 2000150, 4400070, 6200010, 6400070 }, // brand, dark varnish, tsar stone, palladium scrap
        //};
    }

    public class CustomWeapon
    {
        // item ID
        public int New_ItemID;
        public int CloneTarget_ItemID;

        // asset bundle stuff
        public string AssetBundle_Name;
        public string VisualPrefabName;
        public string ItemIconName;

        // actual item stuff
        public string Name;
        public string Description;

        // base itemstats
        public int Durability;
        public int BaseValue;
        public int Weight;

        // EquipmentStats
        public float[] DamageBonuses;
        public float ManaUseModifier;

        // WeaponStats
        public float AttackSpeed;               // 24 = attack speed
        public float Impact;                    // 25 = weapon impact
        public DamageList BaseDamage;           // 26,27,28,29,30,31 = weapon base damage

        // add status effect buildups
        public List<string> hitEffects;

        // custom recipe
        public int[] RecipeIngredientIDs;

        //// weapon
        //public int WeaponType;
        //public bool TwoHand;
        //public bool OffHanded;
    }
}
