import os
import sys
import win32com.client
from typing import List
from configparser import ConfigParser
from PyQt5.QtGui import QIcon
from PyQt5 import QtWidgets
from polars import DataFrame

"""
SHAPES_PATH: str - Nơi lưu trữ ảnh Preview của các Shapes
TRANSLATION_PATH: str - Nơi lưu trữ các file ngôn ngữ
GITHUB_URL: str - Đường dẫn đến Repository trên Github
LANG: str - Ngôn ngữ hiện tại của ứng dụng
"""

# Read Config file
__CONFIG = ConfigParser()
__CONFIG.read("./config.ini")
LANG = __CONFIG.get("Config", "lang")
DEBUG_MODE = __CONFIG.getboolean("Debug", "debug")

# ? Global Constants
GITHUB_URL = "https://github.com/thnhmai06/tao-slide-tot-nghiep"
SHAPES_PATH = os.path.abspath("./temp/shapes/")
TRANSLATION_PATH = os.path.abspath("./translations/")
app = QtWidgets.QApplication(sys.argv)


class PowerPoint:
    def __init__(self):
        super().__init__()
        self.instance = None
        self.prs = None

    def open_instance(self):
        if not self.instance:
            self.instance = win32com.client.Dispatch("PowerPoint.Application")
        return self.instance

    def open_presentation(self, path):
        read_only = True
        has_title = False
        window = False
        if self.instance:
            self.prs = self.instance.Presentations.open(
                path, read_only, has_title, window
            )
        return self.prs

    def close_instance(self) -> bool:
        if self.instance:
            self.instance.Quit()
            self.instance = None
            return True
        return False

    def close_presentation(self) -> bool:
        if self.prs and self.instance:
            self.prs.Close()
            self.prs = None
            return True
        return False


pptx = PowerPoint()


# ? Biến lưu thông tin người dùng nhập vào
class Input:
    class Pptx:
        def __init__(self):
            super().__init__()
            self.path = str()

        def setPath(self, path: str):
            self.path = path

    class Csv:
        def __init__(self):
            super().__init__()
            self.df: DataFrame = None
            self.placeholders = list()
            self.number_of_students = 0

        def get(self, num: int):
            num -= 1  # Convert to 0-based index
            if self.df is not None and 0 <= num < self.number_of_students:
                return self.df[num].to_dict()
            else:
                return None

    class Shapes(list):
        def __init__(self):
            super().__init__()

        def add(self, id: int, image_path: str):
            shape = {"id": str(id), "path": image_path, "icon": QIcon(image_path)}
            self.append(shape)

    class Config:
        class ConfigImage:
            def __init__(self, placeholder: str, shape_id: str):
                super().__init__()
                self.placeholder = placeholder
                self.shape_id = shape_id

        def __init__(self):
            super().__init__()
            self.text: List[str] = []
            self.image: List[Input.Config.ConfigImage] = []

        def add_text(self, text: str):
            self.text.append(text)

        def add_image(self, shape_id: str, placeholder: str):
            config_image = self.ConfigImage(placeholder, shape_id)
            self.image.append(config_image)

    class Save:
        def __init__(self):
            super().__init__()
            self.path: str = None

        def setPath(self, save_path: str):
            self.path = save_path

    def __init__(self):
        self.pptx = self.Pptx()
        self.csv = self.Csv()
        self.shapes = self.Shapes()
        self.config = self.Config()
        self.save = self.Save()


input = Input()
