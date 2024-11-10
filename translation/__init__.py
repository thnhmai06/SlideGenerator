from globals import LANG
import json
__TRANSLATION_PATH = "./translation"

with open(f"{__TRANSLATION_PATH}/{LANG}.json", 'r', encoding='utf-8') as file:
    TRANS = json.load(file)
