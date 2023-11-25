# Against The Storm Cheats

Update 11/24/23: Many new cheat powers added by user Kurdran - thank you so much! You should now consider this mod to be co-authored by both of us :)

The compiled mod is meant to be loaded into the game with [BepInEx](https://github.com/BepInEx/BepInEx). 
I tested with BepInEx_x64_5.4.22.0 on Windows 11 (https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip). 
Simply download the loader and unzip it into your game directory. 
For the Steam version of the game, this will likely be `C:\Program Files (x86)\Steam\steamapps\common\Against the Storm\`. 

The `.dll` file created by this mod (`Josiwe.ATS.Cheats.dll` by default) can then be dropped into the `BepInEx\plugins` subdirectory 
inside the game directory. You might have to make this directory yourself, or run the game once to have it made automatically.

You'll also need the json configuration file Josiwe.ATS.Cheats.Config.json in the same directory. This file can be edited
to selectively enable cheats or modify how they work.


## There's quite a few things you can configure in that json file which we hope you can tweak to your heart's content.

**MoarSeasonRewards**
	Boolean: _true or false_
	- when set to true it maxes out the amount of items displayed for each cornerstone pick to 7 (the max the UI can display without major issues)
	- when set to false uses the game's default logic
**"AllRacesInWorldMap":**
	Boolean: _true or false_
	- when set to true it should allow a more random choice of races when picking a cell in the world map
	- when set to false uses the game's default logic
	- experimental feature
**"EnableWildcardBlueprints":**
	Boolean: _true or false_
	- when set to true it replaces the game's random blueprint logic with wildcard picks; can be used in conjunction with the BlueprintsMultiplier setting
	- when set to false uses the game's default logic
**"EnableInfiniteCornerstoneRerolls":**
	Boolean: _true or false_
	- when set to true it allows the player to reroll cornerstones an infinite amount of times (also updates the UI to always show 99 rerolls left)
	- when set to false uses the game's default logic

**"ResolveMultiplier":**
	Float: _accepts any positive floating point number, usually only one or two decimals (i.e. 1.0, 0.3, 2.45)_
	- used as a multiplier for resolve gains (blue bars for each race at the top left of the map game UI)
	- setting this to any number between 0.0 and 1.0 will make the peon's resolve bar fill at a slower rate
	- setting this to any number higher than 1.0 will make peon's resolve bar fill at a faster rate
	- setting this to 1.0 uses the game's default logic
**"ReputationMutiplier":**
	Float: _accepts any positive floating point number, usually only one or two decimals (i.e. 1.0, 0.3, 2.45)_
	- used as a multiplier for reputation growth (blue bar at the bottom centre of the map game UI)
	- setting this to 0.0 will make it so that _**only**_ positive loyalty map events and uber happy peons can increase your reputation
	- setting this to any number between 0.0 and 1.0 will make the reputation bar fill at a slower rate
	- setting this to any number higher than 1.0 will make the reputation bar fill at a faster rate
	- setting this to 1.0 uses the game's default logic
**"ImpatienceMultiplier":**
	Float: _accepts any positive floating point number, usually only one or two decimals (i.e. 1.0, 0.3, 2.45)_
	- used as a multiplier for impatience growth (red bar at the bottom centre of the map game UI)
	- setting this to any number between 0.0 and 1.0 will make the impatience bar fill at a slower rate
	- setting this to any number higher than 1.0 will make the impatience bar fill at a faster rate
	- setting this to 1.0 uses the game's default logic

**"ReputationStopgap":**
	Integer: _accepts any positive whole number greater than zero (i.e. 1, 3, 5)_
	- used as a hard stop for reputation growth
	- setting this to any positive value greater than zero will prevent the reputation bar from filling up
	- most useful when exploration is your goal (see notes below)
	- setting this to 0 will allow the game's default logic to end your game when the max value for the map is reached (win scenario)
**"ImpatienceStopgap":**
	Integer: _accepts any positive whole number greater than zero (i.e. 1, 3, 5)_
	- used as a hard stop for impatience growth
	- setting this to any positive value greater than zero will prevent the impatience bar from filling up
	- most useful when exploration is your goal (see notes below)
	- setting this to 0 will allow the game's default logic to end your game when the max value for the map is reached (lose scenario)
**"ZoomLimitMultiplier":**
	Integer: _accepts any positive whole number greater than zero (i.e. 1, 3, 5)_
	- allows both the playable map and the world map to be zoomed out by a factor of X (the value you define)
	- great at 3, will work nicely with most machines
	- awesome at 7 or higher, but can slow things down to a crawl on low end machines
**"BlueprintsMultiplier":**
	Integer: _accepts any positive whole number greater than zero (i.e. 1, 3, 5)_
	- when set to any number greater than one, will allow a user to pick more blueprints (after either gaining a whole reputation point or buying the extra blueprint from a vendor)
	- when set to 1 uses the game's default logic
**"CashRewardMultiplier":**
	Integer: _accepts any positive whole number greater than zero (i.e. 1, 3, 5)_
	- when set to any number greater than one, will allow a user to get more money when declining a cornerstone pick
	- when set to 1 uses the game's default logic
**"CornerstonePicksPerSeason": 3**
	Integer: _accepts any positive whole number greater than zero (i.e. 1, 3, 5)_
	- when set to any number greater than one, will allow a user to pick more cornerstones per season
	- when set to 1 uses the game's default logic

### Ideas for explorers:
- If you like exploring maps, and want to skip losing a game due to the queen's impatience buildup, just set _ImpatienceStopgap_ to any postive number above zero
- If you also like exploring maps without loyalty choices/events/rewards ending your game early, just set the _ReputationStopgap_ to any positive number above zero
	- Remember to save a loyalty choice/event/reward (or a few, depending on how high you set the reputation stopgap) so you can pause the game and turn them in at once to finish said map
	- Archaeological dig site maps may require a _ReputationStopgap_ value of 2, if you want to finish all 3 events =)
