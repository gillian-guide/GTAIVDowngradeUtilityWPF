# Gillian's GTA IV Downgrade Utility
Semi-automatically downgrades your GTA IV installation to 1.0.8.0 or 1.0.7.0.

![image](https://github.com/gillian-guide/GTAIVDowngradeUtilityWPF/assets/70141395/912a7380-f153-4ebe-8e44-d2434cf059e9)

## Usage
- Launch the tool.
- Select your game folder, the one that includes `GTAIV.exe`.
- Toggle `Make GFWL-compatible` if you plan to play GFWL.
- Press `Downgrade`.
- Done!

## Features
- Downgrading the game to 1.0.8.0 or 1.0.7.0 - quickly or fully.
- Getting the base essentialls to getting the game to work.
- Backing up the files to be potentially replaced.
- Cleaning up the game folder from potentially incompatible things (`*.asi`, `dsound.dll`)
- Taking the selected toggles in account to automatically adjust logic.
- ~~Downgrading via Steam Depots~~ <- currently in development!

## Contribution
Contribution is highly welcome. I'm poorly experienced with C#, so the current code is extremely clunky and works out of prayers.

## Attribution
Following NuGet packages were used to create this app:

- [Microsoft-WindowsAPICodePack-Shell](https://github.com/contre/Windows-API-Code-Pack-1.1) by rpastric, contre, dahall - allows to create a Choose File dialogue box.
- And Microsoft's official packages such as [System.Management](https://www.nuget.org/packages/System.Management/) for convenience and functional code.
