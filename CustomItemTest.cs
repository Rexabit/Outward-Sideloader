using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;

// NOTE: This is just a test proof-of-concept for custom items. It is highly specific to one item I set up.

namespace SideLoader
{
    public class CustomItemTest : MonoBehaviour
    {
        public SideLoader script;

        public static GameObject CustomItem;

        public void Init()
        {
            foreach (AssetBundle bundle in script.LoadedBundles["mybundle"])
            {
                if (bundle.LoadAsset<GameObject>("Sphere") is GameObject visuals)
                {
                    GameObject.DontDestroyOnLoad(visuals);

                    if (visuals.GetComponent<MeshRenderer>() is MeshRenderer mesh)
                    {
                        mesh.material.mainTexture = script.TextureData["MyTex"];
                    }

                    // add dummy ItemVisual
                    visuals.AddComponent<ItemVisual>();
                }
                else { continue; } // wrong assetbundle

                // clone a dummy item for a base
                Item newItem = Instantiate(ResourcesPrefabManager.Instance.GetItemPrefab(6600220));
                GameObject.DontDestroyOnLoad(newItem);

                // set our visual's parent to our new item
                visuals.transform.parent = newItem.transform;

                // set up item name, ID, etc
                At.SetValue("Test", typeof(Item), newItem, "m_name");
                At.SetValue("Test Description", typeof(Item), newItem, "m_description");
                newItem.ItemID = 6666666;
                newItem.VisualPrefab = visuals.transform;

                // fix localization override
                At.SetValue("", typeof(Item), newItem, "m_localizedName");
                At.SetValue("", typeof(Item), newItem, "m_localizedDescription");

                // fix RPM dictionary
                if (At.GetValue(typeof(ResourcesPrefabManager), null, "ITEM_PREFABS") is Dictionary<string, Item> Items)
                {
                    script.Log("Adding item to RPM dict.");

                    Items.Add(newItem.ItemID.ToString(), newItem);
                    At.SetValue(Items, typeof(ResourcesPrefabManager), null, "ITEM_PREFABS");
                }

                CustomItem = ResourcesPrefabManager.Instance.GetItemPrefab(6666666).gameObject;

                break; // that was the right bundle, stop searching bundles
            }
        }
    }
}
