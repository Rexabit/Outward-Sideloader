using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;
using Localizer;

// NOTE: This is just a test proof-of-concept for custom items. 
// It is highly specific to one item I set up and needs a lot of work before it's ready for release.

namespace SideLoader
{
    public class CustomItemTest : MonoBehaviour
    {
        public SideLoader script;

        public static GameObject CustomItem;
        public static Recipe customRecipe;

        internal void Update()
        {
            if (script.InitDone < 1) { return; }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                if (ItemManager.Instance.GenerateItemNetwork(CustomItem.GetComponent<Item>().ItemID) is Item item)
                {
                    if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
                    {
                        item.transform.position = c.transform.position;
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
                {
                    c.Inventory.RecipeKnowledge.LearnRecipe(customRecipe);
                }
            }
        }

        public void DarkBrandTest()
        {
            // get base object for template
            if (ResourcesPrefabManager.Instance.GetItemPrefab(2000151) is Item origSword) // strange rusted sword
            {
                // clone it, set inactive, and dont destroy on load
                GameObject newSword = Instantiate(origSword.gameObject);
                newSword.SetActive(false);
                GameObject.DontDestroyOnLoad(newSword);

                // get the Item component and set our custom ID first
                Item item = newSword.GetComponent<Item>();
                item.ItemID = 6666665;

                // set name and description
                string name = "Dark Brand";
                string desc = "\"It's actually a Strange Rusted Sword.\"";
                SetNameAndDesc(item, name, desc);

                // clone the visual prefab so we can modify it without affecting the original item
                Transform newVisuals = Instantiate(item.VisualPrefab);
                newVisuals.gameObject.SetActive(false);
                DontDestroyOnLoad(newVisuals);
                item.VisualPrefab = newVisuals;

                // this bit is highly specific to my Dark Brand test. 
                // I'm grabbing the main model from the Strange Rusted Sword VisualPrefab, then setting the MeshRenderer material on that model.
                if (newVisuals.Find("mdl_itm_crystalSwordBrandBroken_c") is Transform t
                    && t.GetComponent<MeshRenderer>() is MeshRenderer renderer)
                {
                    renderer.material.mainTexture = script.TextureData["MyTex2"];
                }

                // set custom icon
                Texture2D icon = script.TextureData["6666665_Dark Brand"];
                Sprite newIcon = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), Vector2.zero);
                At.SetValue(newIcon, typeof(Item), item, "m_itemIcon");

                // fix ResourcesPrefabManager dictionary
                if (At.GetValue(typeof(ResourcesPrefabManager), null, "ITEM_PREFABS") is Dictionary<string, Item> Items)
                {
                    Items.Add(item.ItemID.ToString(), item);
                    At.SetValue(Items, typeof(ResourcesPrefabManager), null, "ITEM_PREFABS");

                    script.Log(string.Format("Added {0} to RPM dictionary.", item.Name));
                }

                // this bit isnt necessary, just keeping track of my custom item for testing
                CustomItem = newSword;

                // ========  make a custom recipe for the item  ========

                Recipe recipe = new Recipe();
                recipe.SetCraftingType(Recipe.CraftingType.Survival);
                recipe.SetRecipeID(66665);
                recipe.SetRecipeIngredients(new RecipeIngredient[]
                {
                    new RecipeIngredient() // iron sword
                    {
                        ActionType = RecipeIngredient.ActionTypes.AddSpecificIngredient,
                        AddedIngredient = ResourcesPrefabManager.Instance.GetItemPrefab(2000010),
                    },
                    new RecipeIngredient() // vendavel's hospitality
                    {
                        ActionType = RecipeIngredient.ActionTypes.AddSpecificIngredient,
                        AddedIngredient = ResourcesPrefabManager.Instance.GetItemPrefab(6600227),
                    }
                });
                recipe.SetRecipeName("Dark Brand");
                recipe.SetRecipeResults(item, 1);
                recipe.Init();

                DontDestroyOnLoad(recipe);

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

                    script.Log("added custom recipe to dict");
                }

                // not necessary
                customRecipe = recipe;
            }
        }

        public void LoadCustomItemTest() // the 'custom sphere item', just an example of using a custom mesh for item visuals.
        {
            foreach (AssetBundle bundle in script.LoadedBundles["mybundle"])
            {
                // check if this asset bundle contains our custom Sphere object
                if (!(bundle.LoadAsset<GameObject>("Sphere") is GameObject visuals))
                { continue; } // wrong assetbundle

                // fix the texture to a custom material loaded by our texture sideloader
                if (visuals.GetComponent<MeshRenderer>() is MeshRenderer mesh)
                {
                    mesh.material.mainTexture = script.TextureData["MyTex"];
                }
                visuals.AddComponent<ItemVisual>();                 // add dummy item visual component
                GameObject.DontDestroyOnLoad(visuals.gameObject);   // set visuals to dontdestroyonload()

                // clone a dummy item for a base. Make sure to SetActive(false)
                GameObject newItem = Instantiate(ResourcesPrefabManager.Instance.GetItemPrefab(2000150).gameObject);
                newItem.SetActive(false);

                // get item component and set ID
                Item item = newItem.GetComponent<Item>();
                item.ItemID = 6666666;

                // set name and description
                string name = "Sphere of Power";
                string desc = "\"Legendary artifact constructed by Cabal Artisans during the Age of the Pink Cube.\""; 
                SetNameAndDesc(item, name, desc);

                // set our visual prefab
                item.VisualPrefab = visuals.transform;

                // set custom icon
                Texture2D icon = script.TextureData["6666666_Test"];
                Sprite newIcon = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), Vector2.zero);
                At.SetValue(newIcon, typeof(Item), item, "m_itemIcon");

                // fix RPM dictionary
                if (At.GetValue(typeof(ResourcesPrefabManager), null, "ITEM_PREFABS") is Dictionary<string, Item> Items)
                {
                    Items.Add(item.ItemID.ToString(), item);            
                    At.SetValue(Items, typeof(ResourcesPrefabManager), null, "ITEM_PREFABS");

                    script.Log("Added item to RPM dict.");
                }

                CustomItem = newItem;
                GameObject.DontDestroyOnLoad(CustomItem);

                break; // that was the right bundle, stop searching bundles
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
}
