# Gillian's GTA IV Downgrade Utility
Semi-automatically downgrades your GTA IV installation to 1.0.8.0 or 1.0.7.0.

![image](https://github.com/gillian-guide/GTAIVDowngradeUtilityWPF/assets/70141395/c9cd1461-c7f7-4c48-bed6-43fb949b1b32)

## Usage
- Launch the tool.
- Select your game folder, the one that includes `GTAIV.exe`.
- Toggle `Make GFWL-compatible` if you don't plan to play with GFWL.
- Toggle other things if your heart desires so. Default options are fine, usually.
- Press `Downgrade`.
- Done!

## Features
- Downgrading the game to 1.0.8.0 or 1.0.7.0 - quickly or fully.
- Installing the base essential mods.
- Backing up the files that could be potentially replaced.
- Cleaning up the game folder from potentially incompatible things (`*.asi`, `dsound.dll`) before downgrading.
- Taking the selected toggles in account to automatically adjust logic (aka, the tool *shouldn't* let you do incompatible selections)

## Toggles that require downloading
Downloads are a one-time only (unless an update is detected for UAL or FF)
- Ultimate ASI Loader (if any `.asi` mods are selected, that being ZolikaPatch, Steam Achievements, FusionFix or XLivelessAddon).
- Downgrade radio.
- Install FusionFix.

## Contribution
Contribution is highly welcome. I'm poorly experienced with C#, so the current code is extremely clunky and works out of prayers. There's also far too many checks that I have no desire to optimize.

## Attribution
Following NuGet packages were used to create this app:

- [Microsoft-WindowsAPICodePack-Shell](https://github.com/contre/Windows-API-Code-Pack-1.1) by rpastric, contre, dahall - allows to create a Choose File dialogue box.
- And Microsoft's official packages such as [System.Management](https://www.nuget.org/packages/System.Management/) for convenience and functional code.
