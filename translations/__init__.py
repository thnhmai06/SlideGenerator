import json
from globals import LANG, TRANSLATION_PATH

with open(f"{TRANSLATION_PATH}/{LANG}.json", "r", encoding="utf-8") as file:
    TRANS = json.load(file)
