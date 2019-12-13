# Outward Sideloader

## Asset SideLoader and Replacer for Outward.

Uses Partiality - https://github.com/PartialityModding/PartialityLauncher/releases

* Credits to Elec0 (https://github.com/Elec0) for the base mod

How to use:

* Unzip the "SideLoader.zip" file directly to your Outward installation directory.
* For Partiality users, enable the mod in Partiality, hit "Apply Mods" and "Yes"
* For BepInEx, move "SideLoader.dll" to "Outward\BepInEx\plugins\\", and install the BepInEx Partiality wrapper.
* That's it! Follow specific instructions below for more info.

## Creating SideLoader Packs ##

For all uses of the SideLoader (replacing textures, custom items, custom asset bundles, etc) you will need to make a SideLoader pack.

An SL Pack is simply a folder with a few sub-folders inside it. Importantly, you must use the correct names and capitalization. The base MyFolderName (name of your SL pack) can contain three folders: AssetBundles, CustomItems (+Recipes) and Texture2D. If you don't use a folder you can delete it if you wish.

```
- MyFolderName
 |- AssetBundles (contains Unity AssetBundle files)
 |- CustomItems (contains .json files)
    |- Recipes (contains .json files)
 |- Texture2D (contains .png files)
```

If you download this repository, an example folder is included to use as a template if you wish.

Once your SL pack is ready, simply place it in Mods\SideLoader\ and it will be applied on launch.

### Replacing Texture .PNGs (Materials) ###
Texture .PNGs can be placed in the Texture2D folder of your SL Pack, the SideLoader will automatically replace the originals by matching the name.

Important: Use uTiny Ripper to unpack the game and get all textures, this way it ensures they are already named correctly.

* Put your texture .PNGs in the "Mods\Resources\Texture2D\" folder
* They must have the exact name that the game uses for them. This file will almost always begin with the prefix "tex_"

Nine Dots use a custom shader, and the material names are set like so:
* "\_MainTex" ("name_d.png" or "name.png") : Albedo (RGB) and Transparency (A).
* "\_NormTex" ("name_n.png"): Normal map (bump map)
* "\_GenTex" ("name_g.png"): Specular (R), Gloss (G), Occlusion (B).
* "\_SpecColorTex" ("name_sc.png") : used to add color to the specular map in some cases (RGB)
* "\_EmissionTex" ("name_i.png" or "name_e.png") : Emissive map (glow map)

When replacing anything other than the Main Texture, you must ALSO include the main texture otherwise no changes will be made. You do not need to make any changes to the main texture.

Note that sometimes the game uses inconsistent names for the different material layers. In this case, all layers should be renamed after the main texture (maintex, \_d.png or no suffix). For example, for Plate Armor, the main texture is "tex_cha_PlateArmorPlain.png", but some of the layers are just "tex_cha_PlateArmor_[suffix].png". The secondary layers in this case would all be renamed to "tex_cha_PlateArmorPlain_[suffix].png" in order for the sideloader to correctly identify them.

### Custom Asset Bundles ###
Modders can use this tool to conveniently load and manage asset bundles, for use in their own C# mods.

* You MUST be on the same version of Unity that the game uses. This is currently 5.6.1, soon will be 2018.4
* Look at this Docs page if you're new to AssetBundles: https://docs.unity3d.com/Manual/AssetBundles-Workflow.html

To use the loaded Asset Bundles in your own C# mods:
* Add a reference to SideLoader.dll (from Outward\Mods\) to your project
* Put "Using SideLoader;" at the top of your C# file
* You can now access the SideLoader from "SL.Instance"
* Put your AssetBundle folder (generated from your Unity Project) in the "AssetBundles" folder of your mod pack.
* Before using your assets, check if SL.Instance.InitDone is greater than 0 first (if SL.Instance.InitDone > 0...)
* SL.Instance.LoadedBundles["bundlename"] will return your asset bundle. 
* Follow standard Unity procedure for instantiating objects from your AssetBundles as needed.

### Custom Items ###
You can define custom items by creating a simple .JSON file in the CustomItems folder, following one of the following templates. All items defined in this folder will be loaded and added to the game's systems automatically.

Note: I have included an example SL Pack folder for custom items, which shows how to set up a weapon. You can place this SL Pack folder in the Mods\SideLoader folder, and have a look how things are set up.

#### Notes about Custom Visual Prefabs ####

The Custom Visual Prefabs are entirely optional. If you do not wish to set one for your custom item, simply set all the relevant fields to blank ("").

Some knowledge about Unity is recommended if you plan on setting up your own custom visuals. You'll need to import an .fbx mesh, create a GameObject prefab out of it, and assign that prefab to an asset bundle. 

Importantly, the custom visuals must:
* Have a BoxCollider (used for world collision, not combat)
* Have a transform scale of 1, 1, 1

All assets which are used for your custom visuals must also be included in the same asset bundle.

#### Item Base (Important) ####
All Custom Items have the following parameters. If you're just defining a generic item, use this template:

```
{
    "New_ItemID": 0000000,
    "CloneTarget_ItemID": 0000000,
    "AssetBundle_Name": "MyBundleFolder",
    "VisualPrefabName": "MyVisualPrefabName",
    "ArmorVisualPrefabName": "MyArmorVisualPrefabName",
    "HelmetHideFace": false,
    "HelmetHideHair": false,
    "ItemIconName": "MyIconName",
    "Visual_PosOffset": {
        "x": 0.0,
        "y": 0.0,
        "z": 0.0
    },
    "Visual_RotOffset": {
        "x": 0.0,
        "y": 0.0,
        "z": 0.0
    },
    "Name": "My Item Name",
    "Description": "My Description.",
    "Durability": 1,
    "BaseValue": 1,
    "Weight": 1.0
}
```

First, we'll define our new Item ID, and the item we will be using for a base.


``New_ItemID`` : The new unique Item ID which your item will use. This will overwrite the existing ID if it exists already.

``CloneTarget_ItemID`` : The Item ID which we will use as a base. Choose one as similar as possible to your item.


The next few settings apply to the custom visuals. If you're not using any, just leave these blank ("")


``AssetBundle_Name`` : The FOLDER NAME which contains your asset bundle for your visual prefab. This folder must be inside the AssetBundles folder.

``VisualPrefabName`` : The UNIQUE PREFAB NAME of your custom item visuals. It can be inside ANY asset bundle in your folder.

``ArmorVisualPrefabName`` : Only for armor. This is the special Skinned Mesh version of your armor visual prefab, used when it is equipped. For weapons and other items, just put "".

``HelmetHideFace`` : For Helmets only. 'true' to hide the player's face when equipped, false to show.

``HelmetHideHair`` : For Helmets only. 'true' to hide hair when equipped, 'false' to show.

``ItemIconName`` : The ICON NAME (without ".png") of your custom item icon. Place it inside the Texture2D folder.

``Visual_PosOffset`` : The Vector3 transform position offset of your visuals, mainly for aligning weapons and armor.

``Visual_RotOffset`` : The Vector3 rotation offset (Quaternion.Euler), also mainly for weapons and armor.


Finally, the real item details:


``Name`` : Your item name

``Description`` : Optional item description

``Durability`` : Item durability stat. Set to -1 for infinite (none).

``BaseValue`` : Item buy price. Sell price is 0.3x buy price.

``Weight`` : Item weight.


#### Custom Equipment ####
You can define a custom equipment such as armor, bags etc. with the following template:
```
{
    "New_ItemID": 0000000,
    "CloneTarget_ItemID": 0000000,
    "AssetBundle_Name": "MyBundleFolder",
    "VisualPrefabName": "MyVisualPrefabName",
    "ArmorVisualPrefabName": "MyArmorVisualPrefabName",
    "HelmetHideFace": false,
    "HelmetHideHair": false,
    "ItemIconName": "MyIconName",
    "Visual_PosOffset": {
        "x": 0.0,
        "y": 0.0,
        "z": 0.0
    },
    "Visual_RotOffset": {
        "x": 0.0,
        "y": 0.0,
        "z": 0.0
    },
    "Name": "My Item Name",
    "Description": "My Description.",
    "Durability": 1,
    "BaseValue": 1,
    "Weight": 1.0,
    "DamageBonuses": [
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0
    ],
    "DamageResistances": [
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0
    ],
    "PhysProtection": 0.0,
    "ImpactResistance": 0.0,
    "ManaUseModifier": 0.0,
    "StaminaUsePenalty": 0.0,
    "MovementPenalty": 0.0,
    "HealthBonus": 0.0,
    "PouchBonus": 0.0,
    "ColdProtection": 0.0,
    "HeatProtection": 0.0
}
```
Note: Armor uses the "ArmorVisualPrefabName" for the SkinnedMeshRenderer version of the armor, when it is equipped.

Aside from the same properties we have from the base item template, we also have some new settings:

``DamageBonuses`` : the Damage Bonus stat for each element (order: Phys, Ethereal, Decay, Lightning, Frost, Fire, n/a, n/a, Raw)

``DamageResistances`` : The Damage Resistance stat for each element, same order as bonuses

``PhysProtection`` : The Physical Protection stat

``ManaUseModifier`` : The Mana Use Modifier stat. For cost reduction, use negative values.

``MovementPenalty`` : Actually "Movement Speed Bonus" in game. For speed bonus, use negative values.

``HealthBonus`` : Not displayed in game. Max HP bonus.

``PouchBonus`` : Pouch capacity bonus.

``ColdProtection`` and ``HeatProtection`` : Weather protection stats

#### Custom Weapons ####

For Custom Weapons, use the following template:

```
{
    "New_ItemID": 0000000,
    "CloneTarget_ItemID": 0000000,
    "AssetBundle_Name": "MyBundleFolder",
    "VisualPrefabName": "MyVisualPrefabName",
    "ArmorVisualPrefabName": "",
    "HelmetHideFace": false,
    "HelmetHideHair": false,
    "ItemIconName": "MyIconName",
    "Visual_PosOffset": {
        "x": 0.0,
        "y": 0.0,
        "z": 0.0
    },
    "Visual_RotOffset": {
        "x": 0.0,
        "y": 0.0,
        "z": 0.0
    },
    "Name": "My Item Name",
    "Description": "My Description.",
    "Durability": 1,
    "BaseValue": 1,
    "Weight": 1.0,
    "DamageBonuses": [
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0
    ],
    "DamageResistances": [
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0
    ],
    "PhysProtection": 0.0,
    "ImpactResistance": 0.0,
    "ManaUseModifier": 0.0,
    "StaminaUsePenalty": 0.0,
    "MovementPenalty": 0.0,
    "HealthBonus": 0.0,
    "PouchBonus": 0.0,
    "ColdProtection": 0.0,
    "HeatProtection": 0.0,
    "AttackSpeed": 1.0,
    "Impact": 1.0,
    "BaseDamage": {
        "m_list": [
            {
                "Type": 0,
                "Damage": 0
            },
            {
                "Type": 1,
                "Damage": 0
            },
        ]
    },
    "hitEffects": [
        "Status Name 1",
        "Status Name 2"
    ]
}
```

This is the same as the Equipment template, but with a few more options.

NOTE: Most of the Equipment Stats cannot be displayed by weapons. Only Damage Bonuses and Mana Use Modifier can be displayed by all weapons, and Shields can also display Impact Resistance.

The new options we have for weapons are:

``AttackSpeed`` : The attack speed stat, default is 1.0. Only used by Melee Weapons.

``Impact`` : The Base Impact damage of the weapon

``BaseDamage`` : I've shown the format to use for two different damage types. If you want more or less, add or remove the { "Type": [number], "Damage": [number] } brackets as needed. 

The "Type" value is the damage type:
* 0 is Physical
* 1 is Ethereal
* 2 is Decay
* 3 is Lightning
* 4 is Frost
* 5 is Fire

``hitEffects``: The Status Effects this weapon will inflict on hit, separated by comma and newlines.

Note that the names used here are not necessarily the actual name displayed by the game.

* "Bleeding"
* "Bleeding +" (Extreme Bleeding)
* "Burning"
* "Poisoned"
* "Poisoned +" (Extreme Poison)
* "Burn"
* "Chill"
* "Curse"
* "Elemental Vulnerability"
* "Haunted"
* "Doom"
* "Pain"
* "Confusion"
* "Dizzy"
* "Cripped"
* "Slow Down"

### Custom Recipes ###

All items (including custom or already existing ones) can have custom recipes defined for them by the SideLoader.

Either define recipes at runtime using C#, or place Recipe JSON files in the "CustomItems\Recipes\\" folder.

#### Recipes at Runtime from C# ####
If you are using the SideLoader as a reference in your C# Mod, you can use the following syntax to define recipes at runtime:

```
using SideLoader;

CustomItems.DefineRecipe(int ItemID, int craftingType, List<int> IngredientIDs);
```

Where ItemID is the ID of the Item rewarded on crafting the Recipe, craftingType is the station type (0 is Alchemy, 1 is Cooking, 2 is None) and IngredientIDs is a List of ints which are your ingredient item IDs.

#### Recipes from .JSON ####

Place your Recipe .json files in "CustomItems\Recipes\".

Template:
```
{
    "Result_ItemID": 0,
    "CraftingType": 2,
    "Ingredient_ItemIDs": [
        0,
        0,
        0,
        0
    ]
}
```

Settings explanation:

``Result_ItemID`` : The ID of the Item which will be rewarded upon crafting the recipe.

`` CraftingType`` : 0 for Alchemy Station, 1 for Cooking Station, 2 for Survival (no station)

``Ingredient_IDs`` : The List of Item IDs which will form your recipe. Make sure to remove unused ID lines if you aren't using all 4.

## Conclusion ##

If you have any more questions, feel free to contact me in the Outard Discord, I'm Sinai#4637.
