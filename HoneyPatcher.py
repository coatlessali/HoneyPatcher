#!/usr/bin/python
from guizero import App, PushButton, Text
from configparser import ConfigParser
import os
import sys
config = ConfigParser()
config.read('HoneyConfig.ini')

# VARS

path = config.get('main', 'path')
print(path)
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
    config.set('main', 'path', directory)
    honey_restart()

# GUI

#logoskip = config.getboolean('main', 'logoskip')

app = App(title="HoneyPatcher: Arcade Stage", bg="#090F10")

message = Text(app, text=f"Logoskip: {logoskip}")
message.text_color = "#e7e7e7"

button = PushButton(app, text="Toggle Logoskip (Disables n_advstf.farc customization)", command=toggle_logoskip)
button.text_color = "#e7e7e7"

select_folder_button = PushButton(app, text="Select STF USRDIR", command=set_directory)
select_folder_button.text_color = "#e7e7e7"

app.display()