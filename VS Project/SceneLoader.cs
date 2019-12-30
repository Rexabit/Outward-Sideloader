using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using OModAPI;
using UnityEngine.SceneManagement;
using SinAPI;

// *****************************************************************************************************************

    // This is a temporary testing class. It is not ready for release yet.

// *****************************************************************************************************************

namespace SideLoader
{
    public class SceneLoader : MonoBehaviour
    {
        public SideLoader _base;

        public List<GameObject> ObjFix = new List<GameObject>();

        private static readonly List<string> ObjFixDict = new List<string>()
        {
            "Environment",
            "AISquadManagerStructure",
            "SpawnPointManager",
            "AudioManager",
            "DefeatScenariosContainer"
        };

        //internal void OnEnable()
        //{
        //    On.NetworkLevelLoader.MidLoadLevel += MidLoadLevelHook;
        //}

        //internal void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.F6))
        //    {
        //        BuildObjFix();

        //        string sceneNames = "";
        //        foreach (string scenePath in _base.FilePaths[ResourceTypes.Scene])
        //        {
        //            if (sceneNames != "") { sceneNames += ", "; }
        //            sceneNames += Path.GetFileNameWithoutExtension(scenePath);
        //        }

        //        OLogger.Log("Loaded Scenes: " + sceneNames + ", trying LoadLevelOneShot!");

        //        LoadScene(_base.FilePaths[ResourceTypes.Scene][0]);
        //    }
        //}

        public void LoadScene(string assetBundlePath) // calls BaseLoadLevel with a custom scene. Just give it the filepath of the asset bundle.
        {
            if (AssetBundle.LoadFromFile(assetBundlePath) is AssetBundle bundle)
            {
                // get the first relative scene path in the bundle.
                // This of course assumes the bundle contains only one Scene
                string scenePath = bundle.GetAllScenePaths()[0];

                // just a shorthand variable
                var loader = NetworkLevelLoader.Instance;

                // the game sets this variable 
                At.SetValue(scenePath, typeof(NetworkLevelLoader), loader, "m_lastLoadedLevelName"); 

                // basic config for a BaseLoadLevel call
                int spawnPoint = -1;
                float spawnOffset = 1.5f;
                bool save = true;
                At.Call(loader, "BaseLoadLevel", new object[] { spawnPoint, spawnOffset, save }); // call it
            }
        }

        private void MidLoadLevelHook(On.NetworkLevelLoader.orig_MidLoadLevel orig, NetworkLevelLoader self)
        {
            OLogger.Warning("Doing ObjFix!");

            // ======== setup Core components ========
            foreach (GameObject obj in ObjFix)
            {
                OLogger.Warning("Instantiating " + obj.name + " fix!");
                var obj2 = Instantiate(obj);
                obj2.SetActive(true);
                
                foreach (string name in ObjFixDict)
                {
                    if (obj2.name.Contains(name))
                    {
                        OLogger.Log("renaming " + obj2.name + " to " + name);
                        obj2.name = name;
                    }
                }

                if (obj2.name.Contains("Environment"))
                {
                    foreach (Transform child in obj2.transform)
                    {
                        if (child.name != "Sky Dome Dark"
                            && child.name != "TerrainProbe"
                            && child.name != "WindZone"
                            && child.name != "ZSpawn1"
                            && child.name != "WeatherEvents")
                        {
                            OLogger.Warning("Destroying " + child.name + " from " + obj2.name);
                            DestroyImmediate(child.gameObject);
                        }
                    }
                }

                if (obj2.name.Contains("AISquadManagerStructure"))
                {
                    foreach (Transform child in obj2.transform)
                    {
                        if (child.name != "Floor"
                            && child.name != "SquadReserve"
                            && child.name != "DeadAIContainer"
                            && child.name != "SquadSpawnPoints")
                        {
                            OLogger.Warning("Destroying " + child.name + " from " + obj2.name);
                            DestroyImmediate(child.gameObject);
                        }
                    }
                }
            }

            OLogger.Log("Done setting up ObjFix");

            orig(self);
        }

        private void BuildObjFix()
        {
            if (ObjFix.Count < 1)
            {
                foreach (string name in ObjFixDict)
                {
                    GameObject obj = GameObject.Find(name);
                    obj.SetActive(false);
                    var obj2 = Instantiate(obj);
                    obj.SetActive(true);

                    obj2.SetActive(false);
                    DontDestroyOnLoad(obj2);
                    ObjFix.Add(obj2);
                }
            }
        }
    }
}
