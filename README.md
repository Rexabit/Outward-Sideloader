# Outward Sideloader

## Asset SideLoader and Replacer for Outward.

Uses Partiality - https://github.com/PartialityModding/PartialityLauncher/releases

* Credits to Elec0 (https://github.com/Elec0) for the base mod

How to use:

* Put the "Resources" folder in "Outward\Mods"
* Put "SinAPI.dll" and "OModAPI.dll" (from the util folder) in "Outward\Outward_Data\Managed\"
* Put "SideLoader.dll" in "Outward\Mods"
* Enable the mod in Partiality (or BepInEx Partiality Wrapper if you use Bep) and launch the game.

To replace Textures:
* Put your texture PNG's in the "Resources\Texture2D\" folder
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

I've included my example of how I generated a basic custom item, with a custom icon, description, name and item ID. It's not replacing any of the items in the game, this is a completely new item which works with all the game systems.

To spawn the example item, press "F6" while in-game.

I've also created a custom recipe for the item. Press F5 to learn the recipe.
