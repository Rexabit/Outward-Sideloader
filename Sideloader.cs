﻿using System;
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

namespace SideLoader
{
    public class SL : PartialityMod // the mod loader, and public class which contains the static SL.Instance (for accessing loaded assets outside this mod)
    {
        public static SideLoader Instance;

        public GameObject obj;
        public string ID = "OTW_SideLoader";
        public double version = 1.0;

        public SL()
        {
            this.author = "Sinai";
            this.ModID = ID;
            this.Version = version.ToString("0.00");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            obj = new GameObject(ID);
            GameObject.DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<SideLoader>();
            Instance._base = this;
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class SideLoader : MonoBehaviour // the actual SideLoader script (for internal use only). For external usage, use SL.Instance
    {
        public SL _base;

        public AssetBundleLoader BundleLoader;
        public AssetReplacer Replacer;

        public CustomItemTest itemtest;

        public int InitDone = -1;    // -1 is unstarted, 0 is initializing, 1 is done
        public bool Loading = false; // for coroutines

        // scene change flag for replacing materials after game loads them
        private string CurrentScene = "";
        private bool SceneChangeFlag;

        // main directory stuff
        public string loadDir = @"Mods\Resources";
        public string[] directories;
        public string[] SupportedFolders = { ResourceTypes.Texture, ResourceTypes.AssetBundle }; // List of supported stuff we can sideload        
        public Dictionary<string, List<string>> FilePaths = new Dictionary<string, List<string>>(); // Category : list of files in category
        
        // textures
        public Dictionary<string, Texture2D> TextureData = new Dictionary<string, Texture2D>();  // FileName : data of texture files

        // asset bundles
        public Dictionary<string, List<AssetBundle>> LoadedBundles = new Dictionary<string, List<AssetBundle>>(); //  Folder Name: list of Asset Bundles in folder

        internal void Update()
        {
            if (InitDone < 0)
            {
                InitDone = 0;
                StartCoroutine(Init());
            }
            else if (InitDone > 0)
            {
                if (CurrentScene != SceneManagerHelper.ActiveSceneName)
                {
                    SceneChangeFlag = true;
                }

                if (Global.Lobby.PlayersInLobbyCount < 1 || !NetworkLevelLoader.Instance.IsOverallLoadingDone) { return; }

                if (SceneChangeFlag)
                {
                    CurrentScene = SceneManagerHelper.ActiveSceneName;
                    SceneChangeFlag = false;

                    StartCoroutine(Replacer.ReplaceActiveAssets());
                }
            }
        }

        private IEnumerator Init()
        {
            Log("Version " + _base.Version + " starting...", 0);

            // Add Components
            BundleLoader = _base.obj.AddComponent<AssetBundleLoader>();
            BundleLoader.script = this;
            Replacer = _base.obj.AddComponent<AssetReplacer>();
            Replacer.script = this;

            // read folders, store all file paths in FilePaths dictionary
            CheckFolders();

            // load texture changes
            Loading = true;
            StartCoroutine(Replacer.LoadTextures());
            while (Loading) { yield return null; } // wait for loading callback to be set to false

            // load asset bundles
            Loading = true;
            StartCoroutine(BundleLoader.LoadAssetBundles());
            while (Loading) { yield return null; }       

            // load something else...

            // Check currently loaded assets and replace what we can
            Loading = true;
            StartCoroutine(Replacer.ReplaceActiveAssets());
            while (Loading) { yield return null; }


            // ========= custom item test =========
            itemtest = _base.obj.AddComponent(new CustomItemTest { script = this });
            itemtest.LoadCustomItemTest();
            // ====================================


            Log("Finished initialization.", 0);
            InitDone = 1;
        }

        private void CheckFolders()
        {
            int i = 0;
            foreach (string dir in SupportedFolders)
            {
                // Make sure we have the key initialized
                if (!FilePaths.ContainsKey(dir))
                    FilePaths.Add(dir, new List<string>());

                string dirPath = loadDir + @"\" + dir;

                if (!Directory.Exists(dirPath))
                    continue;

                bool flag = dir == ResourceTypes.AssetBundle; 

                string[] paths = flag ? Directory.GetDirectories(dirPath) : Directory.GetFiles(dirPath);

                foreach (string s in paths)
                {
                    string assetPath = flag ? new DirectoryInfo(s).Name : new FileInfo(s).Name;
                    FilePaths[dir].Add(assetPath);

                    i++; // add to total asset counter
                }
            }

            Log(string.Format("Found {0} total assets to load.", i));
        }

        // ============== Other misc functions ==============

        public void Log(string log, int errorLevel = -1)
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
        public static string AssetBundle = "AssetBundles";
    }
}
