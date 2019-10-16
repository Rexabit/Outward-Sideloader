# Outward Sideloader

## Asset SideLoader and Replacer for Outward.

Uses Partiality - https://github.com/PartialityModding/PartialityLauncher/releases

* Credits to Elec0 (https://github.com/Elec0) for the base mod

How to use:

* Unzip the "SideLoader.zip" file (in the "! Release" folder) directly to your Outward installation directory.
* That's it! Follow specific instructions below for more info.

To replace Textures:
* Put your texture PNG's in the "Mods\Resources\Texture2D\" folder
* They must have the exact name that the game uses for them. Use UABE or AssetStudio to find names.
* Currently you can edit the main texture (usually \_d for diffuse, or NO \_[letter] at the end)
* You can also edit the \_n (normal or height map) version, but you must also include the main texture as well or no changes will be made.

To load custom asset bundles:
* Add a reference to SideLoader.dll (from Outward\Mods\) to your project
* Put "Using SideLoader;" at the top of your C# file
* You can now access the SideLoader from "SL.Instance"
* Put your AssetBundle folder (generated from your Unity Project) in the "Resources\AssetBundles\" folder. It should look like "Resources\AssetBundles\yourbundlename\ [Asset files here]"
* Before using your assets, check if the "InitDone" int is greater than 0 (SL.Instance.InitDone > 0) first.
* SL.Instance.LoadedBundles["foldername"] will give you a list of AssetBundles in your folder

To load custom textures (for your assets):
* Same way as replacing textures, but use your own custom name.
* SL.Instance.TextureData["filename"] returns the Texture2D of your PNG

## Custom Item Example

* The "Custom Item Template.json" file shows an explanation of how to set up the custom item .json files.
* "Custom Item Example Resources.zip" is a Resources folder (extract to Outward\Mods\\). You can see the structure of how to set up two custom weapons with their asset bundles and custom Texture2D pngs. Open the AssetBundles in Unity to see how to set up these models.
