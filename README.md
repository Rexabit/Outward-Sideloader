# Outward Sideloader

## Asset SideLoader and Replacer for Outward.

Uses Partiality - https://github.com/PartialityModding/PartialityLauncher/releases

* Credits to Elec0 (https://github.com/Elec0) for the base mod

How to use:

* Unzip the "SideLoader.zip" file directly to your Outward installation directory.
* That's it! Follow specific instructions below for more info.

### Replacing Texture .PNGs (Materials) ###
Texture .PNGs can be placed in the Texture2D folder, the SideLoader will automatically replace the originals by matching the name.

* Put your texture .PNGs in the "Mods\Resources\Texture2D\" folder
* They must have the exact name that the game uses for them. 
** I recommend unpacking the game with uTiny ripper and looking in the "Texture2D" folder to get a full list of all texture names, as well as the original PNGs themselves.
* Currently you can edit the main texture (usually "texturename\_d.png", or just "texturename.png")
* You can also edit the \_n.png (normal or height map version), but you must also include the main texture as well or no changes will be made.

### Custom Asset Bundles ###
Modders can use this tool to conveniently load and manage asset bundles, for use in their own C# mods.

If you are using asset bundles to define custom items using the Custom Items feature, see below.

* You MUST be on the same version of Unity that the game uses. This is currently 5.6.1, soon will be 2018.4
* Look at this Docs page if you're new to AssetBundles: https://docs.unity3d.com/Manual/AssetBundles-Workflow.html
* Add a reference to SideLoader.dll (from Outward\Mods\) to your project
* Put "Using SideLoader;" at the top of your C# file
* You can now access the SideLoader from "SL.Instance"
* Put your AssetBundle folder (generated from your Unity Project) in the "Resources\AssetBundles\" folder. It should look like "Resources\AssetBundles\yourbundlename\ [Asset files here]"
* Before using your assets, check if the "InitDone" int is greater than 0 (SL.Instance.InitDone > 0) first.
* SL.Instance.LoadedBundles["foldername"] will give you a list of AssetBundles in your folder
* Follow standard Unity procedure for instantiating objects from your AssetBundles as needed.

### Custom Items and Recipes ###
This SideLoader also has a basic Custom Items and Custom Recipes feature. Currently, Weapons and Equipment have (more or less) full support, and there is basic support for generic items as well.

To define a custom item or recipe, simply use one of the .JSON templates below depending on the type of item you want to create, and place the json file in the CustomItems (or CustomItems\CustomRecipes\\) folder.

All Custom Items have the following parameters. If you're just defining a generic item, use this template:

```
{
    "New_ItemID": 0000000,
    "CloneTarget_ItemID": 0000000,
    "AssetBundle_Name": "MyBundleFolder",
    "VisualPrefabName": "MyVisualPrefabName",
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

First, we'll define our new Item ID and the Item ID of the item we are using for a base.

``New_ItemID`` : The new unique Item ID which your item will use. This will overwrite the existing ID if it exists already.
``CloneTarget_ItemID`` : The Item ID which we will use as a base. Choose one similar to your item if possible.

The next few settings apply to the custom visuals. If you're not using any, just leave these blank ("" or 0)
``AssetBundle_Name`` : The FOLDER NAME where your custom Visual Prefab
``VisualPrefabName`` : The UNIQUE PREFAB NAME of your custom item visuals. It can be inside ANY asset bundle in your folder.
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

##Conclusion##

If you have any more questions, feel free to contact me in the Outard Discord, I'm Sinai#4637.
