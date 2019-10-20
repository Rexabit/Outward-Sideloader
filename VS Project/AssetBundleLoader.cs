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
        public SideLoader script;

        public IEnumerator LoadAssetBundles()
        {
            float start = Time.time;
            SideLoader.Log("Loading Asset Bundles...");

            // get all bundle folders
            foreach (string dir in script.FilePaths[ResourceTypes.AssetBundle])
            {
                //string bundlename = Path.GetFileName();
                List<AssetBundle> list = new List<AssetBundle>();

                // get files that don't end in .meta or .manifest (the actual bundles)
                foreach (string filepath in Directory.GetFiles(script.loadDir + "/AssetBundles/" + dir).Where(x => !x.EndsWith(".manifest") && !x.EndsWith(".meta")))
                {
                    try
                    {
                        var bundle = AssetBundle.LoadFromFile(filepath);
                        if (bundle && bundle is AssetBundle) { list.Add(bundle); }
                    }
                    catch (Exception e)
                    {
                        SideLoader.Log(string.Format("Error loading bundle: {0}\r\nMessage: {1}\r\nStack Trace: {2}", dir, e.Message, e.StackTrace), 1);
                    }
                }

                script.LoadedBundles.Add(dir, list);
                SideLoader.Log(" - Loaded folder: " + dir);
                yield return null;
            }

            script.Loading = false;
            SideLoader.Log("Asset Bundles loaded. Time: " + (Time.time - start));
        }
    }
}
