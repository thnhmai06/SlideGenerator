import os
import pandas as pd
import win32com.client
from configparser import ConfigParser
from PyQt5.QtGui import QIcon

'''
SHAPES_PATH: str - Nơi lưu trữ ảnh Preview của các Shapes
TRANSLATION_PATH: str - Nơi lưu trữ các file ngôn ngữ
GITHUB_URL: str - Đường dẫn đến Repository trên Github
LANG: str - Ngôn ngữ hiện tại của ứng dụng
'''

# Read Config file
__CONFIG = ConfigParser()
__CONFIG.read("./config.ini")
LANG = __CONFIG.get("Config", "lang")
DEBUG_MODE = __CONFIG.getboolean("Debug", "debug")

#? Global Constants
GITHUB_URL = "https://github.com/thnhmai06/tao-slide-tot-nghiep"
SHAPES_PATH = os.path.abspath("./temp/shapes/") 
TRANSLATION_PATH = os.path.abspath("./translations/")
pptx_instance = win32com.client.Dispatch('PowerPoint.Application')

#? Biến lưu thông tin người dùng nhập vào
class Input(dict):
    class Csv(dict):
        def __init__(self):
            super().__init__()
            self.placeholders = list()
            self.students = dict()
    class Shapes(list):
        def __init__(self):
            super().__init__()

        def add(self, id: int, image_path: str):
            shape_image = {"id": str(id), "path": image_path, "icon": QIcon(image_path)}
            self.append(shape_image)
    class Config(dict):
        class ConfigText(str):
            def __init__(self):
                super().__init__()
                self.text: str = None
            def set(self, text: str):
                self.text = text
        class ConfigImage(dict):
            def __init__(self):
                super().__init__()
                self.placeholder: str = None
                self.shape_id: str = None
            def set(self, shape_id: str, placeholder: str):
                self.placeholder = placeholder
                self.shape_id = shape_id

        def __init__(self):
            super().__init__()
            self.text = list()
            self.image = list()
        def add_text(self, text: str):
            config_text = self.ConfigText()
            config_text.set(text)
            self.text.append(text)
        def add_image(self, shape_id: str, placeholder: str):
            config_image = self.ConfigImage()
            config_image.set(placeholder, shape_id)
            self.image.append(config_image) 
    class SavePath(str):
        def __init__(self):
            super().__init__()
            self.save_path: str = None
        def set(self, save_path: str):
            self.save_path = save_path
    
    def __init__(self):
        self._df: pd.DataFrame = None

        self.csv = self.Csv()
        self.shapes = self.Shapes()
        self.config = self.Config()
        self.save_path = self.SavePath()
input = Input()

# TODO: chuyển get sang collect_input.py