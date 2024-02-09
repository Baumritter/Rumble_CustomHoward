## Installation Instructions
1. Install MelonLoader
2. Run Game
3. Drop Mods Folder in to Rumble Source Directory
4. Run the Game and enjoy the mod

## Overview
The Mod tracks which poses are being done by the player and which structures are being spawned/affected by poses/destroyed
Additionally the time 

## File Logging
- Every Pose done by the mod user will be logged
- Every structure within the normal confines of the structure pool wil be tracked (15-20 per type)
- After 1500ms of inactivity a spacer will be created in the log File
- The pose log file is named "PoseLog..."
- The structure log file is named "StructLog..."
- The log files are located in UserData/PoseLogger/Logs/ 

## On Screen Logging
- Only Poses performed by the mod user which do not affect structures or structures which have been affected by the mod user are logged
- The Logging UI will automatically hide itself if not in Gym or park
- After 1500ms of inactivity the lines will reset itself on next input

## Board Logging
- Only Poses performed by the mod user which do not affect structures or structures which have been affected by the mod user are logged
- There are 2 Boards in the gym and 1 in the park
	- At Howard's Place
	- At the ring at the top of the Gym
	- At the central ring in the park
- After 1500ms of inactivity the lines will reset itself on next input

## Controls and Functions for File/Console Logging
- The Logging can be turned Off/On (Default is ON) using the "L" key with the main window of the game selected
- The Logging does only work in the park and in the gym (main hub)

## Known Issues
- The delay tracking of repeated Sprint Poses is inaccurate
- The delay tracking as a host of a park is not accurate (Game Limitation)
- The boards do not have collision. This is intended

## Help And Other Resources
Get help and find other resources in the Modding Discord:
https://discord.gg/fsbcnZgzfa