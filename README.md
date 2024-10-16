# *HoneyPatcher!*

A mod tool for Sonic the Fighters.

Right now, it can:
- Back up your game files
- Restore your game files
- Extract your rom.psarc
- Install packages using vcdiff patches

### Installing Mono (MacOS/Linux/SteamOS)

- MacOS: Install [mono](https://formulae.brew.sh/formula/mono) with [Homebrew](https://brew.sh/).
- Linux: Install mono with your distribution's package manager. If a package / package manager is unavailable, see below.
- Steam Deck / Atomic Linux Distributions: Install [mono](https://formulae.brew.sh/formula/mono) with [Homebrew](https://brew.sh/). **OR** if it is available, install both HoneyFetch and mono inside of a [distrobox](https://github.com/89luca89/distrobox/).

<sub>...because if you don't look sweet, you're not wearing Honey!</sub>

### Creating a Mod
This part sucks (for now) I'm not sure when I'll get to the packing function.
1. Unpack your game files to `template`.
2. Make your changes.
3. Use `xdelta3 -e -s path/to/original.file path/to/modified.file path/to/original.file.vcdiff` to generate patches. Make sure to add the `.vcdiff` to the end.
4. Remove all files that do not have a `.vcdiff` extension from your template.
5. Compress all files in your template (excluding the template folder itself) into a `.zip` file.
