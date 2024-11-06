import os, requests, re
from src.context import infoContext
from src.script import showWidget
import csv

# Thank you github copilot for helping me coding this file hehe :D

class ClientInput:
    def __init__(self):
        self.templatePath = ""
        self.csvPath = ""
        self.savePath = ""
        self.config = {
            "text": [],
            "image": []
        }

def getClientInput(ui):
    input = ClientInput()
    input.templatePath = ui.template_path.text()
    input.csvPath = ui.csv_path.text()
    input.savePath = ui.save_path.text()

    # Get the text configurations
    for index in range(ui.config_text_list.count()):
        input.config["text"].append(ui.config_text_list.item(index).text())

    # Get the image configurations
    for row in range(ui.config_image_table.rowCount()):
        placeholder = ui.config_image_table.item(row, 0).text()
        replaceShape = ui.config_image_table.item(row, 1).text()
        input.config["image"].append({"placeholder": placeholder, "replaceShape": replaceShape})
    return input

def Collect(ui):
    input = getClientInput(ui)
    
