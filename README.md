# Outward-Sideloader

Partiality Asset SideLoader and Replacer for Outward.

* Credits to Elec0 (https://github.com/Elec0) for the base mod

How to use:

* Put the "Resources" folder in "Outward\Mods"
* Put "SinAPI.dll" (from the util folder) in "Outward\Outward_Data\Managed\"
* Put "SideLoader.dll" in "Outward\Mods"

To replace Textures:
* Put your texture PNG's in the "Resources\Texture2D\" folder
* They must have the exact name that the game uses for them. Use UABE or AssetStudio to find names.

To load custom asset bundles:
* Add a reference to SideLoader.dll (from Outward\Mods\) to your project
* Put "Using SL;" at the top of your C# file
* You can now access the SideLoader from "SL.Instance"
* Put your AssetBundle folder (generated from your Unity Project) in the "Resources\AssetBundles\" folder.
* Before using your assets, check if the "InitDone" int is greater than 0 (SL.Instance.InitDone > 0) first.
* SL.Instance.LoadedBundles["foldername"] will give you a list of AssetBundles in your folder

To load custom textures (for your assets):
* Same way as replacing textures, but use your own custom name.
* SL.Instance.TextureData["filename"] returns the Texture2D of your PNG
