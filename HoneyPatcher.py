#!/usr/bin/python
from guizero import App, PushButton, Text, Picture
from configparser import ConfigParser
from pathlib import Path
import os, stat, sys, shutil, platform, subprocess, random, hashlib

# Check if config exists,
# if not make a new one,
# then read config file
if not Path("HoneyConfig.ini").is_file():
    shutil.copy("HoneyConfig.default.ini", "HoneyConfig.ini")
config = ConfigParser()
config.read('HoneyConfig.ini')

paths = ["mods", "template"]
for path in paths:
    if not os.path.exists(path):
        os.mkdir(path)

# set usrdir, logoskip is unused
usrdir = config.get('main', 'usrdir')
logoskip = config.getboolean('main', 'logoskip')
checksum_verif = config.getboolean('main', 'checksum')

# Make psarc executable. There's probably a better way to do this.
if platform.system().lower() != "windows":
    st = os.stat('bin/macosx/psarc')
    os.chmod('bin/macosx/psarc', st.st_mode | stat.S_IEXEC)
    st = os.stat('bin/linux/psarc')
    os.chmod('bin/linux/psarc', st.st_mode | stat.S_IEXEC)
    del st

### DEFS

# Check for mono on MacOS/Linux
def mono_check():
    if not platform.system().lower() == "windows":
        if not shutil.which("mono"):
            app.error("Error", "Could not find mono in your PATH.\nPlease install it - otherwise you won't be able to install farc mods.\n\nPlease refer to the HoneyPatcher GitHub page for more information.")
            return False
        if not shutil.which("xdelta3") and not shutil.which("xdelta"):
            app.error("Error", "Could not find xdelta3 on your system.\nPlease install it - otherwise you won't be able to install mods.\n\nPlease refer to the HoneyPatcher GitHub page for more information.")
            return False

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
        app.error("Hey!", "Your rom.psarc is missing! This indicates your game has been modified. Please install a clean copy of the game so I can make a backup.")
        return False
    else:
        if os.path.exists("BACKUP"):
            shutil.rmtree("BACKUP")
        try:
            shutil.copytree(usrdir, "BACKUP")
        except:
            app.error("Oopsies", "An error occured. This really shouldn't be possible unless permissions are messed up, but anyways...\n\n Ping Ali with this error: backup copy failed")
        else:
            app.info("Notice", "I have created a backup of your game files in a folder named \"BACKUP\" (Please don't mess with it.)")

# Restores your USRDIR backup.
def honey_restore():
    if not os.path.exists("BACKUP"):
        app.error("Error!", "You don\'t have a backup available!")
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
    # Check if a backup was made, create one otherwise.
    # If creation fails for some reason, don't continue.
    if not os.path.exists("BACKUP"):
        if honey_backup() == False:
            return

    # set the path to the rom.psarc
    rom = os.path.abspath(os.path.join(usrdir, "rom.psarc"))
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
    # TODO: test all of this shit on macos
            psarc = os.path.abspath(psarc_rel)
        case _:
            app.error("Oops!", "Your OS is not supported.")
            return
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

# honey mob cemetery
def honey_install():

    # Check if mono/xdelta3 is installed
    if mono_check() == False:
        return

    # Check if game is prepped
    rom = os.path.join(usrdir, "rom.psarc")
    if os.path.exists(rom):
        honey_prep()

    # Everything should go between here and the second shutil.rmtree()
    shutil.rmtree(".tmp", ignore_errors=True)
    if not os.path.exists(".tmp"):
        os.mkdir(".tmp")

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
        if pkg.startswith("."):
            pass
        elif ".zip" in pkg:
            shutil.unpack_archive(os.path.join("mods", pkg), ".tmp")
    
    # Iterate through all vcdiff patches, then apply, then delete (oh boy)
    difflist = []
    for dirpath, dirnames, filenames in os.walk(".tmp"):
        for filename in filenames:
            if ".vcdiff" in filename:
                difflist.append(os.path.join(dirpath, filename))

    patchlist = [] # Intending to use this for logs later
    for diff in difflist:
        # Get the path of the original file, relative to rom_dir
        p = Path(diff)
        removed_toplevel_path = Path(*p.parts[1:])
        removed_extension_path = str(removed_toplevel_path).replace(".vcdiff", "")
        final_path = os.path.join(rom_dir, removed_extension_path)
        # Apply Patches
        if checksum_verif:
            subprocess.run([xdelta, "-d", "-f", "-s", final_path, diff, final_path])
        else: # SPOOKY - forces no checksum verif to allow patch merging (dangerous)
            subprocess.run([xdelta, "-d", "-n", "-f", "-s", final_path, diff, final_path])
        patchlist.append(final_path)

    # Compression
    for fdir in dirlist:
        dirpath = os.path.join(rom_dir, fdir)
        if platform.system().lower() == "windows":
            subprocess.run([farcpack, dirpath])
        else:
            subprocess.run(["mono", farcpack, dirpath])

    app.info("Notice", "Mod installation complete.")

    # Cleanup    
    for fdir in dirlist:
        dirpath = os.path.join(rom_dir, fdir)
        # So for some reason this fails with "texture" 
        # not found if I don't do it this way. 
        # What? Why? If I remove these lines texture remains.
        try:
            shutil.rmtree(dirpath)
        except:
            pass
    
    shutil.rmtree(os.path.join(".", ".tmp"), ignore_errors=True)

def honey_unpack():

    if mono_check() == False:
        return

    rom = os.path.join(usrdir, "rom.psarc")
    if os.path.exists(rom):
        honey_prep()

    rom_dir = os.path.join(usrdir, "rom")
    farclist = ["sprite/n_advstf.farc", "sprite/n_cmn.farc", "sprite/n_cmn.farc", "sprite/n_fnt.farc", "sprite/n_info.farc", "sprite/n_stf.farc", "string_array.farc", "sprite/n_advstf/texture.farc", "sprite/n_cmn/texture.farc", "sprite/n_fnt/texture.farc", "sprite/n_info/texture.farc", "sprite/n_stf/texture.farc"]
    farcpack = os.path.join(".", "bin/mono/FarcPack.exe")

    for farc in farclist:
        farcpath = os.path.join(rom_dir, farc)
        if platform.system().lower() == "windows":
            subprocess.run([farcpack, farcpath])
        else:
            subprocess.run(["mono", farcpack, farcpath])

    if not os.path.exists("template"):
        os.makedirs("template")

    shutil.copytree(rom_dir, "template", dirs_exist_ok=True)

def checksum_off():
    checksum = app.yesno("WARNING", "Disabling checksum verification can allow installing hand-made, merged patches. However, these are prone to breakage. If something is wrong with your patch, it will be applied anyways.\n\nDo not come crying to me if it blows up in your face.\n\nDo you wish to continue?")
    if checksum:
        config.set('main', 'checksum', "false")
        checksum_on_button.disable()
        checksum_on_button.visible = False
        checksum_off_button.enable()
        checksum_off_button.visible = True
        honey_restart()
    else:
        return

def checksum_on():
    config.set('main', 'checksum', "true")
    checksum_off_button.disable()
    checksum_off_button.visible = False
    checksum_on_button.enable()
    checksum_on_button.visible = True
    honey_restart()

### GUI

app = App(title="HoneyPatcher: Arcade Stage", bg="#090F10")

logo = Picture(app, image="assets/HONEYBADGER.png")

magic_number = random.randrange(0, 99) # 1 in 100 chance of explode.png
if magic_number == 1:
    logo.image = "assets/explode.png"

select_folder_button = PushButton(app, text="Select USRDIR...", command=set_directory)
select_folder_button.text_color = "#e7e7e7"

# automated backups, still can be reenabled if you uncomment the below two lines
#backup_button = PushButton(app, text="Backup USRDIR...", command=honey_backup)
#backup_button.text_color = "#e7e7e7"

restore_button = PushButton(app, text="Restore USRDIR...", command=honey_restore)
restore_button.text_color = "#e7e7e7"

prepare_button = PushButton(app, text="Prepare USRDIR for mods...", command=honey_prep)
prepare_button.text_color = "#e7e7e7"

install_button = PushButton(app, text="Install all mods...", command=honey_install)
install_button.text_color = "#e7e7e7"

unpack_button = PushButton(app, text="Unpack game files into template...", command=honey_unpack)
unpack_button.text_color = "#e7e7e7"

checksum_on_button = PushButton(app, command=checksum_off, text="Checksum Verification: ON")
checksum_on_button.text_color = "#2aa198"

checksum_off_button = PushButton(app, command=checksum_on, text="Checksum Verification: OFF")
checksum_off_button.text_color = "#dc322f"

if checksum_verif == True:
    checksum_off_button.disable()
    checksum_off_button.visible = False
    checksum_on_button.enable()
    checksum_on_button.visible = True
else:
    checksum_off_button.enable()
    checksum_off_button.visible = True
    checksum_on_button.disable()
    checksum_on_button.visible = False

app.display()
