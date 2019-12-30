# Outward Sideloader

## Asset SideLoader and Replacer for Outward.

Uses Partiality - https://github.com/PartialityModding/PartialityLauncher/releases

* Credits to Elec0 (https://github.com/Elec0) for the base mod

How to use:

* Unzip the "SideLoader.zip" file directly to your Outward installation directory.
* For Partiality users, enable the mod in Partiality, hit "Apply Mods" and "Yes"
* For BepInEx, move "Outward\Mods\SideLoader.dll" to "Outward\BepInEx\plugins\\", and install the BepInEx Partiality wrapper.
* That's it! Follow specific instructions below for more info.

## Examples and Resources ##

* First, download this repository (download to zip, or clone)
* Follow the instructions above to install the SideLoader correctly
* Look in the Outward\Mods\SideLoader folder, there should be an "ExampleFolder" which shows the structure of a SideLoader pack.
* Back in the main repository, the "Resources" folder has some useful files such as a Custom Weapon example, some blank icon templates, the human model rig for Outward, and probably more stuff in the future. 

## Creating SideLoader Packs ##

For all uses of the SideLoader (replacing textures, custom items, custom asset bundles, etc) you will need to make a SideLoader pack.

An SL Pack is simply a folder with a few sub-folders inside it. Importantly, you must use the correct names and capitalization. The base MyFolderName (name of your SL pack) can contain up to four folders: 
* "AssetBundles" _(contains Unity AssetBundle files)_
* "CustomItems" _(contains .json files)_
* "CustomItems\Recipes" _(contains .json files)_
* "Texture2D" _(contains .png files)_

If you don't use a folder you can delete it if you wish. The structure should look like this:

```
- MyFolderName
 |- AssetBundles 
 |- CustomItems 
    |- Recipes 
 |- Texture2D 
```

If you download this repository, an example folder is included to use as a template if you wish.

Once your SL pack is ready, simply place it in Mods\SideLoader\ and it will be applied on launch.

## Wiki ##

View the Wiki (link on the top) for this repository for more in-depth guides.

## Conclusion ##

If you have any more questions, feel free to contact me in the Outard Discord, I'm Sinai#4637.
