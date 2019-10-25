using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

namespace SideLoader
{
    public class AssetReplacer : MonoBehaviour
    {
        public SideLoader script; 

        public IEnumerator ReplaceActiveAssets()
        {
            SideLoader.Log("Replacing active assets...");
            float start = Time.time;

            // ============ materials ============
            var list = Resources.FindObjectsOfTypeAll<Material>()
                        .Where(x => x.mainTexture != null && script.TextureData.ContainsKey(x.mainTexture.name))
                        .ToList();

            SideLoader.Log(string.Format("Found {0} materials to replace.", list.Count));

            int i = 0;
            foreach (Material m in list)
            {
                string name = m.mainTexture.name;
                i++; SideLoader.Log(string.Format(" - Replacing material {0} of {1}: {2}", i, list.Count, name));

                // set maintexture (diffuse map)
                m.mainTexture = script.TextureData[name];

                // ======= set other shader material layers =======     
                if (name.EndsWith("_d")) { name = name.Substring(0, name.Length - 2); } // try remove the _d suffix, if its there

                // check each shader material suffix name
                foreach (KeyValuePair<string, string> entry in Suffixes)
                {
                    if (script.TextureData.ContainsKey(name + entry.Key))
                    {
                        SideLoader.Log(" - Setting " + entry.Value + " for " + m.name);
                        m.SetTexture(entry.Value, script.TextureData[name + entry.Key]);
                    }
                }

                yield return null;
            }

            // ============ something else... ============

            // ==============================================

            SideLoader.Log("Active assets replaced. Time: " + (Time.time - start), 0);
            script.Loading = false;
        }

        private static readonly Dictionary<string, string> Suffixes = new Dictionary<string, string>()
        {
            { "_n", "_NormTex" },
            { "_g", "_GenTex" },
            { "_m", "_GenTex" },
            { "_sc", "_SpecColorTex" },
            { "_e", "_EmissionTex" },
            { "_i", "_EmissionTex" },
        };

        public IEnumerator LoadTextures()
        {
            SideLoader.Log("Reading Texture2D data...");
            float start = Time.time;

            var filesToRead = script.FilePaths[ResourceTypes.Texture];

            foreach (string file in filesToRead)
            {
                string fullPath = script.loadDir + @"\" + ResourceTypes.Texture + @"\" + file;
                if (!File.Exists(fullPath))
                    continue;

                Texture2D texture2D = LoadPNG(fullPath);

                script.TextureData.Add(Path.GetFileNameWithoutExtension(file), texture2D);

                SideLoader.Log(" - Texture loaded: " + file);

                yield return null;
            }

            script.Loading = false;
            SideLoader.Log("Textures loaded. Time: " + (Time.time - start), 0);
        }

        public static Texture2D LoadPNG(string filePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }
            return tex;
        }
    }    
}
