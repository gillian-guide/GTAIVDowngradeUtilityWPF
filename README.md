# Gillian's GTA IV Downgrade Utility
Semi-automatically downgrades your GTA IV installation to 1.0.8.0 or 1.0.7.0.

## Singleplayer options
![image](https://github.com/user-attachments/assets/abf60cc3-0896-4fd1-aaad-1825b04a520f)
![image](https://github.com/user-attachments/assets/b8de14a8-f780-4a92-8481-20d5f89b2fb5)

## Multiplayer options
![image](https://github.com/user-attachments/assets/df47f057-bba5-4b6c-8e63-9864eaa4246b)
![image](https://github.com/user-attachments/assets/c8da88b2-369b-4fd0-818f-8009db093efb)


## Usage
- Launch the tool.
- Select your game folder, the one that includes `GTAIV.exe`.
- If you plan to downgrade for singleplayer usage, stay on the same page. Otherwise, press `Switch to multiplayer`.
- Toggle things if your heart desires so. Default options are fine, usually. Don't use advanced mode unless you really need to.
- Press `Downgrade` and read the pop-out prompts if any appear.
- Done!

## Features
- Downgrading the game to 1.0.8.0 or 1.0.7.0 - quickly or fully.
- Separated options for singleplayer and multiplayer-oriented downgrading.
- Installing the base essential mods.
- Automatically backing up the files that could be potentially replaced.
- Cleaning up the game folder from potentially incompatible files before downgrading (they can be found in the backup).
- Taking the selected toggles in account to automatically adjust logic (aka, the tool *shouldn't* let you do incompatible selections)

## Features that require downloading
Downloads are a one-time only (unless an update is detected for UAL or FF)
- Full downgrade
- Downgrade radio
- Any of the mods

## Contribution
Contribution is highly welcome. I'm poorly experienced with C#, so the current code is extremely clunky and works out of prayers. There's also far too many checks that I have no desire to optimize.

## Attribution
Following NuGet packages were used to create this app:

- [Microsoft-WindowsAPICodePack-Shell](https://github.com/contre/Windows-API-Code-Pack-1.1) by rpastric, contre, dahall - allows to create a Choose File dialogue box.
- [RedistributableChecker](https://github.com/bitbeans/RedistributableChecker) by bitbeans - saves some effort checking for installed VC++ redist.
- And Microsoft's official packages such as [System.Management](https://www.nuget.org/packages/System.Management/) for convenience and functional code.
