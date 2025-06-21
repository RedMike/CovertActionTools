# CovertActionTools

Tools for inspecting/modifying game files for the 1990 MicroProse video game Sid Meier's Covert Action.

Repository does not include any game assets or functionality from the game itself.

The files this editor produces can be used to modify or replace the game files and therefore modify some
of the visuals/text/etc in-game. Some of the visuals/text/etc data for the game is embedded into the
EXE files, and therefore is not currently modifiable by this editor 
(see [issue #1](https://github.com/RedMike/CovertActionTools/issues/1)).

Other than visual/text changes, functionality in the game is in general not modifiable using this editor 
as it is all hard-coded into the EXE files, so only things supported by the game engine are possible 
(e.g. changing how crimes happen, amount of score received, flight durations, etc). Some logic could
be set up to be modifiable (e.g. mission sets) but is not currently modifiable by this editor
(see [issue #1](https://github.com/RedMike/CovertActionTools/issues/1)).

## Obtaining the game

This repository does not include the game data! You must have a copy of the game in order to use this project.

**You should obtain the game legally via one of the licensed distributors**. For example:

* [GOG.com](https://www.gog.com/en/game/sid_meiers_covert_action)
* [Steam](https://store.steampowered.com/app/327390/Sid_Meiers_Covert_Action_Classic/)

## Components

A .NET library called CovertActionTools.Core is included which is used for the actual data parsing/exporting,
as well as including some convenient processors to produce at least debug output based on the data.
This library can be used to build custom scripts/applications that modify files programmatically.

CovertActionTools.App is a desktop application which you can use to parse a game install, modify, or inspect
assets from it, and then publish a set of files that have been modified, to distribute as a mod. **Because
the files potentially contain some retail game data, these files should be modified to be distributed as 
a binary patch instead of directly uploading the files** (an internal implementation of this is tracked in 
[issue #2](https://github.com/RedMike/CovertActionTools/issues/2)).

## Usage

To use the editor:

1. Download the latest release and run `CovertActionTools.App.exe`
2. Click `File` > `Parse Game Install` and select the game install `MPS` folder as the Source Path, 
and an empty folder as the Destination Path, then click Load; once the load finishes successfully, click Save
3. Click `File` > `Open Package` and select the folder you used as the Destination Path, then click Load
4. You have now loaded the package, make any changes you want, then `File` > `Save Package` to save the package,
and `File` > `Publish Package` to export any changed files to a new folder for distribution/using in a game install.

Important to note: published packages will only contain files that have been changed in the package, you can see
this list by clicking the top-level element in Package Explorer.

## Documentation

There are a number of documentation files available that explain some of the file layouts/game quirks/formats:

* [CRIME File Format](https://github.com/RedMike/CovertActionTools/blob/main/docs/crime-file-format.md)

## Acknowledgements

* [CanadianAvenger.io](https://canadianavenger.io/) for lots of patient help in figuring out the PAN file format, as 
well as confirming/correcting some of the PIC file quirks.
* [Jari Komppa's git repository](https://github.com/jarikomppa/covert_action/tree/master?tab=readme-ov-file) for giving
me a place to start on some of the formats.