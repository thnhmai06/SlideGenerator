import os
import pythoncom
from typing import List
import win32com.client
from polars import DataFrame
from PyQt5.QtGui import QIcon

class Input:
    class Path:
        def __init__(self):
            super().__init__()
            self.path = str()

        def setPath(self, path: str):
            self.path = os.path.abspath(path)

    class Csv:
        def __init__(self):
            super().__init__()
            self.df: DataFrame = None
            self.placeholders = list()
            self.number_of_students = 0

        def get(self, num: int):
            num -= 1  # Convert to 0-based index
            if self.df is not None and 0 <= num < self.number_of_students:
                return self.df[num].to_dicts()
            else:
                return None

    class Shape:
        def __init__(self, shape_index: int, image_path: str):
            super().__init__()
            self.shape_index = shape_index
            self.image_path = image_path
            self.icon = QIcon(image_path)
    class Shapes(list[Shape]):
        def __init__(self):
            super().__init__()

        def add(self, shape_index: int, image_path: str):
            shape = Input.Shape(shape_index, image_path)
            self.append(shape)
    class Config:
        class ConfigImage():
            def __init__(self, placeholder: str, shape_index: int):
                super().__init__()
                self.placeholder = placeholder
                self.shape_index = shape_index

        def __init__(self):
            super().__init__()
            self.text: List[str] = []
            self.image: List[Input.Config.ConfigImage] = []

        def add_text(self, text: str):
            self.text.append(text)

        def add_image(self, shape_index: int, placeholder: str):
            config_image_item = self.ConfigImage(placeholder, shape_index)
            self.image.append(config_image_item)

    def __init__(self):
        self.pptx = self.Path()
        self.csv = self.Csv()
        self.shapes = self.Shapes()
        self.config = self.Config()
        self.save = self.Path()

class PowerPoint:
    def __init__(self):
        super().__init__()
        self.instance: win32com.client.CDispatch = None
        self.presentation = None

    def open_instance(self):
        # Tạo môi trường COM cho thread
        pythoncom.CoInitialize()

        if not self.instance:
            try:
                self.instance = win32com.client.Dispatch("PowerPoint.Application")
            except Exception as e:  # noqa: F841
                self.instance = None
        return self.instance

    def open_presentation(self, path, read_only=False):
        MSOFALSE = 0
        MSOTRUE = -1

        read_only = MSOTRUE if read_only else MSOFALSE
        has_title = MSOFALSE
        window = MSOFALSE
        if self.instance and not self.presentation:
            try:
                # Mở File PowerPoint
                self.presentation = self.instance.Presentations.open(
                    path, read_only, has_title, window
                )

                # Nếu file mở ở read-write mode, và đang ở Final State
                if read_only == MSOFALSE and self.presentation.Final:
                    self.presentation.Final = False

                return None
            except Exception as e:
                self.presentation = None
                return e

    def close_instance(self) -> bool:
        # Không cần close instance, vì
        # - Nếu đang mở một file pptx khác thì close instance sẽ đóng cả file đó
        # - instance sẽ tự đóng khi không có presentation nào đang được mở  

        # if self.instance:
        #     self.instance.Quit()
        #     self.instance = None
        #     return True
        # return False

        # Giải phóng môi trường COM
        try: 
            pythoncom.CoUninitialize()
            return True
        except Exception:
            return False

    def close_presentation(self) -> bool:
        if self.presentation and self.instance:
            self.presentation.Close()
            self.presentation = None
            return True
        return False

class ProgressLogLevel:
    INFO = "INFO"
    ERROR = "ERROR"