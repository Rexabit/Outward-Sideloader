using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;
using Localizer;

// NOTE: This is just a test proof-of-concept for custom items. It is highly specific to one item I set up and needs a lot of work.

    // It would be better to add a reference to the SideLoader from another script, and use SL.Instance.LoadedBundles to find your asset bundle.

namespace SideLoader
{
    public class CustomItemTest : MonoBehaviour
    {
        public SideLoader script;

        public static GameObject CustomItem;

        internal void Update()
        {
            if (script.InitDone > 0 && Input.GetKeyDown(KeyCode.F6))
            {
                if (ItemManager.Instance.GenerateItemNetwork(CustomItem.GetComponent<Item>().ItemID) is Item item)
                {
                    if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
                    {
                        item.transform.position = c.transform.position;
                    }

                    item.gameObject.SetActive(true);
                    item.LoadedVisual.gameObject.SetActive(true);
                }
            }
        }

        public void LoadCustomItemTest()
        {
            foreach (AssetBundle bundle in script.LoadedBundles["mybundle"])
            {
                // check if this asset bundle contains our custom Sphere object
                if (bundle.LoadAsset<GameObject>("Sphere") is GameObject visuals)
                {
                    // fix the texture to a custom material loaded by our texture sideloader
                    if (visuals.GetComponent<MeshRenderer>() is MeshRenderer mesh)
                    {
                        mesh.material.mainTexture = script.TextureData["MyTex"];
                    }

                    // add dummy ItemVisual
                    visuals.AddComponent<ItemVisual>();

                    // dont destroy on load
                    GameObject.DontDestroyOnLoad(visuals.gameObject);
                }
                else { continue; } // wrong assetbundle

                // clone a dummy item for a base. Make sure to SetActive(false)
                GameObject newItem = Instantiate(ResourcesPrefabManager.Instance.GetItemPrefab(6600220).gameObject);
                newItem.SetActive(false);

                // get item component
                Item item = newItem.GetComponent<Item>();

                item.ItemID = 6666666;

                // set name and description
                string name = "Sphere of Power";
                string desc = "\"Legendary artifact constructed by Cabal Artisans during the Age of the Pink Cube.\"";

                At.SetValue(name, typeof(Item), item, "m_name");

                // localized name and description           
                ItemLocalization loc = new ItemLocalization(name, desc);

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
    }
}
