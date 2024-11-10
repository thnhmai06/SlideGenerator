import os
import pandas as pd
from ui import menu, progress
from configparser import ConfigParser

_CONFIG_PATH = "./config.ini"
CONFIG = ConfigParser()
CONFIG.read(_CONFIG_PATH)

#? Constants
GITHUB_URL = "https://github.com/thnhmai06/tao-slide-tot-nghiep"
SHAPES_PATH = os.path.dirname("./temp/shapes/")
LANG = CONFIG.get("Config", "lang")

#? Global Variables
placeholders = list()

#? Global functions
# A Function Load fields from csv file and put it in placeholders using pandas
def import_csv_fields(csv_path: str) -> None:
    df = pd.read_csv(csv_path)
    fields = df.columns.tolist()
    for field in fields:
        placeholders.append(field)

def import_template():
    if not os.path.exists(SHAPES_PATH):
        os.makedirs(SHAPES_PATH)

    # TODO: Download all shapes to SHAPES_PATH here
