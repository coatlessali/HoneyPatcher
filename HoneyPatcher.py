#!/usr/bin/python
from guizero import App, PushButton, Text, Picture
from configparser import ConfigParser
from pathlib import Path
import os
import stat
import sys
import shutil
import platform
import subprocess

### INIT
# Stuff that always should run on startup

if not Path("HoneyConfig.ini").is_file(): # Check if config exists
    shutil.copy("HoneyConfig.default.ini", "HoneyConfig.ini") # If not, make a new one
config = ConfigParser()
config.read('HoneyConfig.ini') # Read config file

match platform.system().lower():
    case "darwin":
        st = os.stat('bin/macosx/psarc')
        os.chmod('bin/macos/psarc', st.st_mode | stat.S_IEXEC)
    case "linux":
        st = os.stat('bin/linux/psarc')
        os.chmod('bin/linux/psarc', st.st_mode | stat.S_IEXEC)
    case _:
        pass
        

### VARS
usrdir = config.get('main', 'usrdir')
print(usrdir)
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
            app.error("What?")
            return
        if os.path.exists(usrdir):
            shutil.rmtree(usrdir)
        try:
            shutil.copytree("BACKUP", usrdir)
        except:
            app.error("Oopsies", "write error here later")
        else:
            app.info("Notice", "Restore complete!")

# TODO: will be used to prep the game files for modding.
# Will require a psarc tool, and a farc tool.
# psarc tool prob won't be too hard, farc tool is a different story
# maybe implement the ability to target an already existing farcpack installation and wrapper?
def honey_prep():
    # Check if a backup was made, give user the option to skip creation of one
    if not os.path.exists("BACKUP"):
        createbackup = app.yesno("Notice", "You don\'t have a backup available to restore. Would you like to create one now?")
        if createbackup:
            honey_backup()
        else:
            fuckitweball = app.yesno("WARNING!", "The tool will not be able to restore your game to an unmodified state unless a backup is present. Are you sure you wish to continue?")
            if not fuckitweball:
                return

    # set the path to the rom.psarc
    rom = os.path.join(usrdir, "rom.psarc")
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
            print(psarc)
        case "darwin":
            psarc_rel = os.path.join(".", "bin/macosx/psarc")
            psarc = os.path.abspath(psarc_rel)
            print(psarc)
            app.info("NOTICE", "This hasn't been tested, there may be bugs!")
        case "windows":
            psarc_rel = os.path.join(".", "bin/win32/PSArcTool.exe")
            psarc = os.path.abspath(psarc_rel)
            print(psarc)
            app.info("NOTICE", "This hasn't been tested, there may be bugs")
            return
        case _:
            app.error("Oops!", "Your OS is not supported.")
            return
    # TODO: test all of this shit on macos/windows
    wd = os.getcwd()
    os.chdir(usrdir)
    try:
        match platform.system().lower():
            case "windows":
                subprocess.run([psarc, rom])
            case _:
                subprocess.run([psarc, '-x', rom])
    except:
        app.error("Oopsies!", "Something went wrong trying to extract rom.psarc.")
        return
    else:
        os.remove(rom)
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
    #global directory
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

### GUI

app = App(title="HoneyPatcher: Arcade Stage", bg="#090F10")

logo = Picture(app, image="assets/explode.png")

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

reset_button = PushButton(app, text="Reset Configuration...", command=reset_config)
reset_button.text_color = "#e7e7e7"

app.display()