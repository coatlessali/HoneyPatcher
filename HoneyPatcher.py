#!/usr/bin/python
from guizero import App, PushButton, Text, Picture
from configparser import ConfigParser
from pathlib import Path
import os
import sys
import shutil

# INIT

configfile = Path("HoneyConfig.ini")
if not configfile.is_file():
    shutil.copy("HoneyConfig.default.ini", "HoneyConfig.ini")
config = ConfigParser()
config.read('HoneyConfig.ini')

# VARS

usrdir = config.get('main', 'usrdir')
print(usrdir)
logoskip = config.getboolean('main', 'logoskip')

# DEFS

def honey_write():
    with open('HoneyConfig.ini', 'w') as configfile:
        config.write(configfile)

def honey_restart():
    honey_write()
    app.info("info", "HoneyPatcher will now restart to apply changes.")
    os.execv(sys.executable, ['python'] + sys.argv)


def toggle_logoskip():
    print("logoskip button pressed")
    if logoskip == True:
        config.set('main', 'logoskip', 'false')
    else:
        config.set('main', 'logoskip', 'true')
    honey_restart()

def set_directory():
    #global directory
    directory = app.select_folder(title="Select Sonic The Fighters USRDIR", folder=".")
    config.set('main', 'usrdir', directory)
    honey_restart()

# GUI

#logoskip = config.getboolean('main', 'logoskip')

app = App(title="HoneyPatcher: Arcade Stage", bg="#090F10")

logo = Picture(app, image="explode.png")

message = Text(app, text=f"Logoskip: {logoskip}")
message.text_color = "#e7e7e7"

button = PushButton(app, text="Toggle Logoskip", command=toggle_logoskip)
button.text_color = "#e7e7e7"

select_folder_button = PushButton(app, text="Select USRDIR...", command=set_directory)
select_folder_button.text_color = "#e7e7e7"

app.display()