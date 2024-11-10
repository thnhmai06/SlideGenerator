import os
from configparser import ConfigParser

# Read Config file
_CONFIG_PATH = "./config.ini"
CONFIG = ConfigParser()
CONFIG.read(_CONFIG_PATH)

#? Global Constants
'''
SHAPES_PATH: str - Nơi lưu trữ ảnh Preview của các Shapes
GIT_URL: str - Đường dẫn đến Repository trên Github
LANG: str - Ngôn ngữ hiện tại của ứng dụng
'''
GITHUB_URL = "https://github.com/thnhmai06/tao-slide-tot-nghiep"
SHAPES_PATH = os.path.dirname("./temp/shapes/") 
LANG = CONFIG.get("Config", "lang")

#? Global Variables
'''
placeholders: list - Danh sách các placeholders/fields có trong file csv
'''
placeholders = list()
