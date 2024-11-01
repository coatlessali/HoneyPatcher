# *HoneyPatcher!*

A mod tool for Sonic the Fighters.

Right now, it can:
- Back up your game files
- Restore your game files
- Extract your rom.psarc
- Install mod packages

Planned features:
- A rewrite in Godot that will have no external dependencies
- Packing templates into packages
- Check and warn for file conflicts

### How do I install this?
Check [the Wiki](https://github.com/coatlessali/HoneyPatcher/wiki/Install-&-Usage-Guide).

### I'm on Steam Deck. What do I do?
Check [here](https://github.com/coatlessali/HoneyPatcher/wiki/Install-&-Usage-Guide#install-on-steam-deck-kde-plasma).

### I'm on MacOS, what do I do?
Check [here](https://github.com/coatlessali/HoneyPatcher/wiki/Install-&-Usage-Guide#install-on-macos).

### Creating a Mod
1. Unpack your game files to `template`.
2. Make your changes.
3. Remove any files you did *not* modify.
4. Go into the `template` folder, and add all files to a `.zip` archive.

### Credits
HoneyPatcher uses a few external programs to manage the game's compressed file archives.
- [psarc](https://ferb.fr/ps3/PSARC/) by Ferb. No license was provided and the software is precompiled for ease of use. No copyright infringement was intended.
- [UnPSARC](https://github.com/rm-NoobInCoding/UnPSARC) by [NoobInCoding](https://github.com/rm-NoobInCoding), licensed under the [MIT License](https://github.com/rm-NoobInCoding/UnPSARC?tab=MIT-1-ov-file#readme).
- [FarcPack](https://github.com/blueskythlikesclouds/MikuMikuLibrary/releases) from [MikuMikuLibrary](https://github.com/blueskythlikesclouds/MikuMikuLibrary) by [Skyth](https://github.com/blueskythlikesclouds). Licensed under the [MIT License](https://github.com/blueskythlikesclouds/MikuMikuLibrary?tab=MIT-1-ov-file).

<sub>*...because if you don't look sweet, you're not wearing Honey!*</sub>
