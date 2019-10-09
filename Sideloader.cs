using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Partiality.Modloader;
using UnityEngine;
using System.IO;
using OModAPI;
using SinAPI;

// Credits to Elec0 for the initial framework

namespace Sideloader
{
    public class ModBase : PartialityMod
    {
        public double version = 1.0;
        public SideLoader script;
        public GameObject obj;

        public ModBase()
        {
            this.author = "Sinai and Elec0";
            this.ModID = "Outward Sideloader";
            this.Version = version.ToString("0.00");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            obj = new GameObject("OTW_SideLoader");
            GameObject.DontDestroyOnLoad(obj);

            script = obj.AddComponent<SideLoader>();
            script._base = this;
            script.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class SideLoader : MonoBehaviour
    {
        public ModBase _base;
        private bool InitDone = false;
        private bool Loading = false;

        private string CurrentScene = "";
        private bool SceneChangeFlag;

        // main directory stuff
        private readonly string loadDir = @"Mods\Resources";
        public string[] directories;
        public string[] SupportedFolders = { ResourceTypes.Texture }; // List of supported stuff we can sideload        
        public Dictionary<string, List<string>> FilePaths = new Dictionary<string, List<string>>(); // Category : list of files in category  
        
        // textures
        public Dictionary<string, Texture2D> TextureData = new Dictionary<string, Texture2D>();  // FileName : data of texture files
        public List<string> ChangedTextures = new List<string>(); // only need to change material assets once.

        public void Init()
        {
            Log("Version" + _base.Version + " starting...", 0);

            // read all folders, store all filepaths in dictionary readData
            CheckFolders();
        }

        internal void Update()
        {
            if (!InitDone)
            {
                InitDone = true;
                StartCoroutine(LoadAssets());
            }
            else if (CurrentScene != SceneManagerHelper.ActiveSceneName)
            {
                SceneChangeFlag = true;
            }

            if (Global.Lobby.PlayersInLobbyCount < 1 || !NetworkLevelLoader.Instance.IsOverallLoadingDone) { return; }

            if (SceneChangeFlag)
            {
                CurrentScene = SceneManagerHelper.ActiveSceneName;
                SceneChangeFlag = false;

                StartCoroutine(ReplaceActiveAssets());
            }
        }

        private IEnumerator LoadAssets()
        {
            float start = Time.time;
            Log("Loading Assets...");

            // load texture changes
            Loading = true;
            StartCoroutine(LoadTextures());

            // wait for loading callback to be set to false
            while (Loading) { yield return null; }

            Log("Assets loaded. Time: " + (Time.time - start));

            // Check currently loaded assets and replace what we can
            Loading = true;
            StartCoroutine(ReplaceActiveAssets());

            while (Loading) { yield return null; }

            Log("Finished initialization.", 0);
        }

        private IEnumerator ReplaceActiveAssets()
        {
            // materials
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            Log("Found " + materials.Count() + " active materials");

            foreach (Material m in materials.Where(x => x.mainTexture != null))
            {
                if (TextureData.ContainsKey(m.mainTexture.name))
                {
                    if (!ChangedTextures.Contains(m.mainTexture.name)) { ChangedTextures.Add(m.mainTexture.name); } // add to dict

                    Log("Replacing material texture: " + m.mainTexture.name);
                    m.mainTexture = TextureData[m.mainTexture.name];
                }
            }

            Log("Finished replacing active assets.", 0);

            yield return null; // doesnt yield between each item, meaning this is not really a coroutine, it may as well be a void. leaving for now.
            Loading = false;
        }

        // ============= FILE LOADING =============

        private void CheckFolders()
        {
            Log("Loading file paths...");

            foreach (string curDir in SupportedFolders)
            {
                // Make sure we have the key initialized
                if (!FilePaths.ContainsKey(curDir))
                    FilePaths.Add(curDir, new List<string>());

                string curPath = loadDir + @"\" + curDir;

                if (!Directory.Exists(curPath))
                    continue;

                string[] files = Directory.GetFiles(curPath);

                // Get the names of the files without having to parse stuff
                foreach (string s in files)
                {
                    FileInfo f = new FileInfo(s);
                    FilePaths[curDir].Add(f.Name);

                    Log(" - Added filepath: " + f.Name);
                }
            }
        }

        private IEnumerator LoadTextures()
        {
            Log("Reading image data...");

            var filesToRead = FilePaths[ResourceTypes.Texture];

            foreach (string file in filesToRead)
            {
                // Make sure the file we're trying to read actually exists (it should but who knows)
                string fullPath = loadDir + @"\" + ResourceTypes.Texture + @"\" + file;
                if (!File.Exists(fullPath))
                    continue;

                Texture2D texture2D = LoadPNG(fullPath);

                TextureData.Add(Path.GetFileNameWithoutExtension(file), texture2D);

                Log(" - Texture loaded: " + file);

                yield return null;
            }

            Loading = false;
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

        // ============== other ==============

        private void Log(string log, int errorLevel = -1)
        {
            log = "[SideLoader] " + log;
            if (errorLevel == 1)
            {
                OLogger.Error(log);
                Debug.LogError(log);
            }
            else if (errorLevel == 0)
            {
                OLogger.Warning(log);
                Debug.Log(log);
            }
            else if (errorLevel == -1)
            {
                OLogger.Log(log);
                Debug.Log(log);
            }
        }
    }

    public static class ResourceTypes
    {
        public static string Texture = "Texture2D";
    }
}
