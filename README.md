# Against The Storm Cheats

The compiled mod is meant to be loaded into the game with [BepInEx](https://github.com/BepInEx/BepInEx). 
I tested with BepInEx_x64_5.4.22.0 on Windows 11 (https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip). 
Simply download the loader and unzip it into your game directory. 
For the Steam version of the game, this will likely be `C:\Program Files (x86)\Steam\steamapps\common\Against the Storm\`. 

The `.dll` file created by this mod (`Josiwe.ATS.Cheats.dll` by default) can then be dropped into the `BepInEx\plugins` subdirectory 
inside the game directory. You might have to make this directory yourself, or run the game once to have it made automatically.

You'll also need the json configuration file Josiwe.ATS.Cheats.Config.json in the same directory. This file can be edited
to selectively enable cheats or modify how they work.