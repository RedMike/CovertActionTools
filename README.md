# CovertActionTools

Tools for inspecting/modifying game files for the 1990 MicroProse video game Sid Meier's Covert Action. Repository 
does not include any game assets or functionality from the game itself, you must provide a valid game install to 
load the game data files.

## Features

- [x] Modify most images in the game
- [x] Modify all animations in the game
- [x] Modify some text in the game
- [x] Modify all crime data
- [x] Modify all clue data
- [x] Modify all world data
- [x] Modify fonts
- [ ] Modify mission set data including crime victim/location strings
(see [issue #1](https://github.com/RedMike/CovertActionTools/issues/1))
- [ ] Modify other text in the game including menu options
(see [issue #1](https://github.com/RedMike/CovertActionTools/issues/1))
- [ ] Modify some images including telegram/bulletin background
(see [issue #7](https://github.com/RedMike/CovertActionTools/issues/7))
- [ ] Modify sounds (will likely never be done, very complicated)
- [ ] Modify almost all game logic (would require a complete rebuild of the game)

## Obtaining the game

This repository does not include the game data! You must have a copy of the game in order to use this project.

**You should obtain the game legally via one of the licensed distributors**. For example:

* [GOG.com](https://www.gog.com/en/game/sid_meiers_covert_action)
* [Steam](https://store.steampowered.com/app/327390/Sid_Meiers_Covert_Action_Classic/)

## Usage

To use the editor:

1. Download [the latest release](https://github.com/RedMike/CovertActionTools/releases) and run `CovertActionTools.App.exe`
2. Click `File` > `Parse Game Install` and select the game install `MPS` folder as the Source Path, 
and an empty folder as the Destination Path, then click Load; once the load finishes successfully, click Save
3. Click `File` > `Open Package` and select the folder you used as the Destination Path, then click Load
4. You have now loaded the package, make any changes you want, then `File` > `Save Package` to save the package,
and `File` > `Publish Package` to export any changed files to a new folder for distribution/using in a game install.

Important to note: published packages will only contain files that have been changed in the package, you can see
this list by clicking the top-level element in Package Explorer.

## Building/Downloading

To build the project yourself, download the project and run `dotnet build`, or open the `.sln` file with any 
relevant IDE (Visual Studio Community, Jetbrains Rider). The built project will be in the `bin` folder. Dependencies 
will be automatically downloaded, which is managed by NuGet.

To get the pre-built binaries, [use the releases page](https://github.com/RedMike/CovertActionTools/releases).
A Github pipeline will automatically publish new versions when changes are made.

## Components

A .NET library called CovertActionTools.Core is included which is used for the actual data parsing/exporting,
as well as including some convenient processors to produce at least debug output based on the data.
This library can be used to build custom scripts/applications that modify files programmatically.

CovertActionTools.App is a desktop application which you can use to parse a game install, modify, or inspect
assets from it, and then publish a set of files that have been modified, to distribute as a mod. **Because
the files potentially contain some retail game data, these files should be modified to be distributed as
a binary patch instead of directly uploading the files** (an internal implementation of this is tracked in
[issue #2](https://github.com/RedMike/CovertActionTools/issues/2)).


## Documentation

There are a number of documentation files available that explain some of the file layouts/game quirks/formats:

* [CRIME File Format](https://github.com/RedMike/CovertActionTools/blob/main/docs/crime-file-format.md)
* More TODO

## Acknowledgements

* [CanadianAvenger.io](https://canadianavenger.io/) for lots of patient help in figuring out the PAN file format, as 
well as confirming/correcting some of the PIC file quirks.
* [Jari Komppa's git repository](https://github.com/jarikomppa/covert_action/tree/master?tab=readme-ov-file) for giving
me a place to start on some of the formats.