using System;
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
        public SideLoader _base;

        public IEnumerator LoadAssetBundles()
        {
            float start = Time.time;
            SideLoader.Log("Loading Asset Bundles...");

            // get all bundle folders
            foreach (string filepath in _base.FilePaths[ResourceTypes.AssetBundle])
            {
                try
                {
                    var bundle = AssetBundle.LoadFromFile(filepath);

                    if (bundle // not sure if necessary, just to be safe
                        && bundle is AssetBundle)
                    {
                        _base.LoadedBundles.Add(Path.GetFileNameWithoutExtension(filepath), bundle);

                        SideLoader.Log(" - Loaded bundle: " + filepath);
                    }
                }
                catch (Exception e)
                {
                    SideLoader.Log(string.Format("Error loading bundle: {0}\r\nMessage: {1}\r\nStack Trace: {2}", filepath, e.Message, e.StackTrace), 1);
                }

                yield return null;
            }

            _base.Loading = false;
            SideLoader.Log("Asset Bundles loaded. Time: " + (Time.time - start));
        }
    }
}
