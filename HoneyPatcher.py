#!/usr/bin/python
from guizero import App, PushButton, Text, Picture
from configparser import ConfigParser
from pathlib import Path
import os, stat, sys, shutil, platform, subprocess, random

### INIT - Stuff that should always run on startup

if not Path("HoneyConfig.ini").is_file(): # Check if config exists
    shutil.copy("HoneyConfig.default.ini", "HoneyConfig.ini") # If not, make a new one
config = ConfigParser()
config.read('HoneyConfig.ini') # Read config file

# Make psarc executable. There's probably a better way to do this.
match platform.system().lower():
    case "darwin":
        st = os.stat('bin/macosx/psarc')
        os.chmod('bin/macos/psarc', st.st_mode | stat.S_IEXEC)
    case "linux":
        st = os.stat('bin/linux/psarc')
        os.chmod('bin/linux/psarc', st.st_mode | stat.S_IEXEC)
    case _:
        pass

# Check for mono on MacOS/Linux
def mono_check():
    if not platform.system().lower() == "windows":
        if not shutil.which("mono"):
            app.error("Error", "Could not find mono in your PATH.\nPlease install it - otherwise you won't be able to install farc mods.\n\nPlease refer to the HoneyPatcher GitHub page for more information.")
            return False
        if not shutil.which("xdelta3") and not shutil.which("xdelta"):
            app.error("Error", "Could not find xdelta3 on your system.\nPlease install it - otherwise you won't be able to install mods.\n\nPlease refer to the HoneyPatcher GitHub page for more information.")
            return False

### VARS
usrdir = config.get('main', 'usrdir')
logoskip = config.getboolean('main', 'logoskip')

### DEFS

# Writes the config. Used automatically in honey_restart()
def honey_write():
    with open('HoneyConfig.ini', 'w') as configfile:
        config.write(configfile)

# Acts as a way to reload the config.
def honey_restart():
    honey_write()
    app.info("info", "HoneyPatcher will now restart to apply changes.")
    os.execv(sys.executable, ['python'] + sys.argv)

# Backs up your USRDIR.
def honey_backup():
    rom = os.path.join(usrdir, "rom.psarc")
    if not Path(rom).is_file():
        app.error("Hey!", "Your rom.psarc is missing! This indicates your game has been modified. Please install a clean copy of the game before backing up.")
    else:
        confirm_backup = app.yesno("Warning!", "Please make sure you have a CLEAN copy of Sonic The Fighters.\nIf you\'re using RPCS3, you can simply delete the game and reinstall your PKG file.\n\nAre you sure you have a clean copy of the game, and want to continue? This will take up ~86 MB of additional storage.")
        if confirm_backup:
            if os.path.exists("BACKUP"):
                shutil.rmtree("BACKUP")
            try:
                shutil.copytree(usrdir, "BACKUP")
            except:
                app.error("Oopsies", "An error occured. This really shouldn't be possible unless permissions are messed up, but anyways...\n\n Ping Ali with this error: backup copy failed")
            else:
                app.info("Notice", "Backup complete! It will be in a folder named \"BACKUP\" (Please don't mess with it.)")

# Restores your USRDIR backup.
def honey_restore():
    if not os.path.exists("BACKUP"):
        app.error("You don\'t have a backup available!")
        return
    confirm_restore = app.yesno("Warning!", "This will erase your USRDIR and restore a backup. This will effectively uninstall all of your USRDIR mods and restore the game to a vanilla state.\n\nDo you want to continue?")
    if confirm_restore:
        if os.getcwd() == usrdir:
            app.error("why did you put the app here")
            return
        if os.path.exists(usrdir):
            shutil.rmtree(usrdir)
        try:
            shutil.copytree("BACKUP", usrdir)
        except:
            app.error("Oopsies", "write error here later")
        else:
            app.info("Notice", "Restore complete!")

def honey_prep():
    # Check if a backup was made, give user the option to skip creation of one
    if not os.path.exists("BACKUP"):
        createbackup = app.yesno("Notice", "You don\'t have a backup available to restore. Would you like to create one now?")
        if createbackup:
            honey_backup()
        else:
            fuckitweball = app.yesno("WARNING!", "The tool will not be able to restore your game to an unmodified state unless a backup is present. It only takes a few seconds. Are you sure you wish to continue?")
            if not fuckitweball:
                return

    # set the path to the rom.psarc
    rom = os.path.abspath(os.path.join(usrdir, "rom.psarc"))
    print(rom)
    # if rom.psarc doesn't exist, assume game is already prepped
    if not os.path.exists(rom):
        app.error("Error!", "rom.psarc not found! Your game is either already prepped, or corrupted!")
        return

    # set path for the psarc tool
    match platform.system().lower():
        case "linux":
            psarc_rel = os.path.join(".", "bin/linux/psarc")
            psarc = os.path.abspath(psarc_rel)
        case "darwin":
            psarc_rel = os.path.join(".", "bin/macosx/psarc")
            psarc = os.path.abspath(psarc_rel)
            app.info("NOTICE", "This hasn't been tested, there may be bugs!")
        case "windows":
            psarc_rel = os.path.join(".", "bin/win32/UnPSARC.exe")
            psarc = os.path.abspath(psarc_rel)
        case _:
            app.error("Oops!", "Your OS is not supported.")
            return
    # TODO: test all of this shit on macos
    wd = os.getcwd()
    os.chdir(usrdir)
    try:
        match platform.system().lower():
            case "windows":
                subprocess.run([psarc, rom])
            case _:
                subprocess.run([psarc, '-x', rom])
    except Exception as e:
        print(e)
        app.error("Oopsies!", "Something went wrong trying to extract rom.psarc.")
        return
    else:
        os.remove(rom)
        if platform.system().lower() == "windows":
            shutil.copytree("rom_Unpacked", "rom", dirs_exist_ok=True)
            shutil.rmtree("rom_Unpacked")
        app.info("NOTICE", "Game files have been prepped for modding!")
    finally:
        os.chdir(wd)        

# Handles whether or not to patch the eboot for logoskip
def toggle_logoskip():
    print("logoskip button pressed")
    if logoskip == True:
        config.set('main', 'logoskip', 'false')
    else:
        config.set('main', 'logoskip', 'true')
    honey_restart()

# Should be set to the USRDIR, for US this is dev_hdd0/game/NPUB30927/USRDIR
def set_directory():
    app.info("Notice", "Please select the USRDIR folder for your game.")
    directory = app.select_folder(title="Select Sonic The Fighters USRDIR", folder=usrdir)
    try:
        config.set('main', 'usrdir', directory)
    except:
        app.info("Notice", "Selection cancelled.")
    else:
        honey_restart()

def reset_config():
    reset = app.yesno("WARNING!", "This will delete your config file and reset it to its default values. This will not delete your backup, if any. Are you sure you want to continue?")
    if reset:
        # TODO: this doesn't work, figure out why
        os.remove('HoneyConfig.ini')
        honey_restart()

def honey_install():
    # honey mob cemetery
    if mono_check() == False:
        return

    # Everything should go between here and the second shutil.rmtree()
    shutil.rmtree(".tmp", ignore_errors=True)
    if not os.path.exists(".tmp"):
        os.makedirs(".tmp")

    # Vars for Extraction/Compression
    rom_dir = os.path.join(usrdir, "rom")
    farclist = ["sprite/n_advstf.farc", "sprite/n_cmn.farc", "sprite/n_cmn.farc", "sprite/n_fnt.farc", "sprite/n_info.farc", "sprite/n_stf.farc", "string_array.farc", "sprite/n_advstf/texture.farc", "sprite/n_cmn/texture.farc", "sprite/n_fnt/texture.farc", "sprite/n_info/texture.farc", "sprite/n_stf/texture.farc"]
    dirlist = ["sprite/n_advstf/texture", "sprite/n_cmn/texture", "sprite/n_cmn/texture", "sprite/n_fnt/texture", "sprite/n_info/texture", "sprite/n_stf/texture", "string_array", "sprite/n_advstf", "sprite/n_cmn", "sprite/n_fnt", "sprite/n_info", "sprite/n_stf"]
    farcpack = os.path.join(".", "bin/mono/FarcPack.exe")

    # Extraction
    for farc in farclist:
        farcpath = os.path.join(rom_dir, farc)
        # TODO: test this shit on windows and macos
        if platform.system().lower() == "windows":
            subprocess.run([farcpack, farcpath])
        else:
            subprocess.run(["mono", farcpack, farcpath])

    #app.info("Extraction", "Extracted FARC files.")

    # ALL OF THE FILE PATCHING / REPLACEMENT LOGIC WILL GO HERE
    # use xdelta3 for patching rom_code, ui assets and music/sounds can be provided in their entirety
    if platform.system().lower() == "windows":
        xdelta = os.path.join(".", "bin/win32/xdelta.exe")
    else:
        if shutil.which("xdelta3"):
            xdelta = "xdelta3"
        else:
            xdelta = "xdelta"

    # Check through zip files
    pkglist = []
    for (dirpath, dirnames, filenames) in os.walk("mods"):
        pkglist.extend(filenames)
        break
    for pkg in pkglist:
        if ".zip" in pkg:
            shutil.unpack_archive(os.path.join("mods", pkg), ".tmp")
    #print(pkglist)
    
    # Iterate through all vcdiff patches, then apply, then delete (oh boy)
    difflist = []
    for dirpath, dirnames, filenames in os.walk(".tmp"):
        for filename in filenames:
            if ".vcdiff" in filename:
                difflist.append(os.path.join(dirpath, filename))
    #print(difflist)

    patchlist = []
    for diff in difflist:
        p = Path(diff)
        #print("p:", p)
        removed_toplevel_path = Path(*p.parts[1:])
        #print("removed_toplevel_path:", removed_toplevel_path)
        removed_extension_path = str(removed_toplevel_path).replace(".vcdiff", "")
        final_path = os.path.join(rom_dir, removed_extension_path)
        subprocess.run([xdelta, "-n", "-d", "-f", "-s", final_path, diff, final_path])
        #print(xdelta, "-d", "-f", "-s", final_path, diff, final_path)
        patchlist.append(final_path)
    #print(patchlist)

    # Apply patches

    app.info("TODO", "put all file patching / replacement logic here")

    # Compression
    for fdir in dirlist:
        dirpath = os.path.join(rom_dir, fdir)
        if platform.system().lower() == "windows":
            subprocess.run([farcpack, dirpath])
        else:
            subprocess.run(["mono", farcpack, dirpath])

    app.info("Notice", "Repacked FARC files.")

    # Cleanup    
    for fdir in dirlist:
        dirpath = os.path.join(rom_dir, fdir)
        # So for some reason this fails with "texture" not found if I don't do it this way. What? Why? If I remove these lines texture remains.
        try:
            shutil.rmtree(dirpath)
        except:
            pass
    
    app.info("Notice", "Cleaned up directories.")

    #shutil.rmtree(".tmp", ignore_errors=True)

def honey_pack():
    if mono_check() == False:
        return
    app.info("todo", "ali needs to write the specification for .stf packages first")

### gato explotano


### GUI

app = App(title="HoneyPatcher: Arcade Stage", bg="#090F10")

logo = Picture(app, image="assets/HONEYBADGER.png")
magic_number = random.randrange(0, 100)
print(magic_number)
if magic_number == 1:
    logo.image = "assets/explode.png"

message = Text(app, text=f"Logoskip: {logoskip}")
message.text_color = "#e7e7e7"

button = PushButton(app, text="Toggle Logoskip", command=toggle_logoskip)
button.text_color = "#e7e7e7"

select_folder_button = PushButton(app, text="Select USRDIR...", command=set_directory)
select_folder_button.text_color = "#e7e7e7"

backup_button = PushButton(app, text="Backup USRDIR...", command=honey_backup)
backup_button.text_color = "#e7e7e7"

restore_button = PushButton(app, text="Restore USRDIR...", command=honey_restore)
restore_button.text_color = "#e7e7e7"

prepare_button = PushButton(app, text="Prepare USRDIR for mods...", command=honey_prep)
prepare_button.text_color = "#e7e7e7"

install_button = PushButton(app, text="Install all mods...", command=honey_install)
install_button.text_color = "#e7e7e7"

pack_button = PushButton(app, text="Pack template into mod...", command=honey_pack)
pack_button.text_color = "#e7e7e7"

reset_button = PushButton(app, text="Reset Configuration...", command=reset_config)
reset_button.text_color = "#e7e7e7"

app.display()
