﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

namespace SideLoader
{
    public class AssetBundleLoader : MonoBehaviour
    {
        public static AssetBundleLoader Instance;

        internal void Awake()
        {
            Instance = this;
        }

        public IEnumerator LoadAssetBundles()
        {
            float start = Time.time;
            SideLoader.Log("Loading Asset Bundles...");

            // get all bundle folders
            foreach (string filepath in SL.Instance.FilePaths[ResourceTypes.AssetBundle])
            {
                try
                {
                    var bundle = AssetBundle.LoadFromFile(filepath);

                    if (bundle // not sure if necessary, just to be safe
                        && bundle is AssetBundle)
                    {
                        SL.Instance.LoadedBundles.Add(Path.GetFileNameWithoutExtension(filepath), bundle);

                        SideLoader.Log(" - Loaded bundle: " + filepath);
                    }
                }
                catch (Exception e)
                {
                    SideLoader.Log(string.Format("Error loading bundle: {0}\r\nMessage: {1}\r\nStack Trace: {2}", filepath, e.Message, e.StackTrace), 1);
                }

                yield return null;
            }

            SL.Instance.Loading = false;
            SideLoader.Log("Asset Bundles loaded. Time: " + (Time.time - start));
        }
    }
}
