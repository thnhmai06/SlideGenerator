import os
import pandas as pd
import win32com.client
from configparser import ConfigParser

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
SHAPES_PATH = os.path.dirname("./temp/shapes/") 
TRANSLATION_PATH = os.path.dirname("./translations/")
pptx_instance = win32com.client.Dispatch('PowerPoint.Application')

#? Global Variables
class CSV_data(dict):
    def __init__(self):
        super().__init__()
        self.placeholders = list()
        self.students = dict()
        self._df = pd.DataFrame()
    def load(self, csv_path) -> None:
        self._df = pd.read_csv(csv_path)
    def get(self) -> bool:
        __number_of_students = len(self._df)
        if not __number_of_students>=1:
            return False
        
        self.placeholders = self._df.columns.tolist()
        self.students = self._df.to_dict(orient='records')
        return True
csv_file = CSV_data()


    