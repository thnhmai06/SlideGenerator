import os
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
TRANSLATION_PATH = os.path.abspath("./translations/")

# ? Global Variables
user_input = Input() 
