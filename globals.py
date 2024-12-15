import os
import sys
from PyQt5.QtWidgets import QApplication
from configparser import ConfigParser
from classes.models import Input

"""
SHAPES_PATH: str - Nơi lưu trữ các Shapes ảnh
TRANSLATION_PATH: str - Nơi lưu trữ các file ngôn ngữ
GITHUB_URL: str - Đường dẫn đến Repository trên Github
LANG: str - Ngôn ngữ hiện tại của ứng dụng
"""

# Read Config file
__CONFIG = ConfigParser()
__CONFIG.read("./settings.ini")
LANG = __CONFIG.get("Config", "lang")
DEBUG_MODE = __CONFIG.getboolean("Debug", "debug")
TIMEOUT = __CONFIG.getint("Config", "timeout")

# ? Global Configurations
GITHUB_URL = "https://github.com/thnhmai06/tao-slide-tot-nghiep"
SHAPES_PATH = os.path.abspath("./temp/shapes/")
DOWNLOAD_PATH = os.path.abspath("./temp/downloads/")
LOG_PATH = os.path.abspath("./logs/")
TRANSLATION_PATH = os.path.abspath("./translations/")

# ? Global Variables
app = QApplication(sys.argv)
user_input = Input() 
