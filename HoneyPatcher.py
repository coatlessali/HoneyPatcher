#!/usr/bin/python
from guizero import App, PushButton, Text
from configparser import ConfigParser
import tkinter as tk
from tkinter import filedialog
import os
import sys
config = ConfigParser()
config.read('HoneyConfig.ini')

# VARS


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

# GUI

logoskip = config.getboolean('main', 'logoskip')

app = App(title="HoneyPatcher: Arcade Stage")

message = Text(app, text=f"Logoskip: {logoskip}")

button = PushButton(app, text="Toggle Logoskip (Disables n_advstf.farc customization)", command=toggle_logoskip)

app.display()