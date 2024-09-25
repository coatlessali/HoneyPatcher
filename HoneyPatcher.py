#!/usr/bin/python
from guizero import App, PushButton, Text, Picture
from configparser import ConfigParser
from pathlib import Path
import os
import sys
import shutil

### INIT
# Stuff that always should run on startup

if not Path("HoneyConfig.ini").is_file(): # Check if config exists
    shutil.copy("HoneyConfig.default.ini", "HoneyConfig.ini") # If not, make a new one
config = ConfigParser()
config.read('HoneyConfig.ini') # Read config file

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
    app.info("TODO", "Implement this")

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

app.display()