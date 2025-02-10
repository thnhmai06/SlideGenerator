import os
import pythoncom
from typing import List
import win32com.client
from polars import DataFrame
from PyQt5.QtGui import QIcon

class Input:
    """
    Lớp chứa các thông tin đầu vào.

    Attributes:
        pptx (Input.Path): Đường dẫn đến file PowerPoint.
        csv (Input.Csv): Thông tin về file CSV.
        shapes (Input.Shapes): Danh sách các Shapes lấy từ file pptx.
        config (Input.Config): Cấu hình thay thế.
        save (Input.Path): Đường dẫn lưu file kết quả.
    """
    class Path:
        """
        Lớp chứa đường dẫn file.

        Attributes:
            path (str): Đường dẫn file.
        """
        def __init__(self):
            self.path = str()

        def setPath(self, path: str):
            """
            Thiết lập đường dẫn file.

            Args:
                path (str): Đường dẫn file.
            """
            self.path = os.path.abspath(path)

    class Csv:
        """
        Lớp chứa thông tin về file CSV.

        Attributes:
            df (DataFrame): DataFrame quản lí dữ liệu file CSV.
            placeholders (list): Danh sách các placeholder.
            number_of_students (int): Số lượng sinh viên.
        """
        def __init__(self):
            self.df: DataFrame = None
            self.placeholders = list()
            self.number_of_students = 0

        def get(self, num: int):
            """
            Lấy thông tin sinh viên theo chỉ số.

            Args:
                num (int): Chỉ số của sinh viên.

            Returns:
                dict: Thông tin của sinh viên.
            """
            num -= 1  # Chuyển đổi sang chỉ số bắt đầu từ 0
            if self.df is not None and 0 <= num < self.number_of_students:
                return self.df[num].to_dicts()
            return None

    class Shape:
        """
        Lớp chứa thông tin về Shape.

        Attributes:
            shape_index (int): Chỉ số của Shape.
            image_path (str): Đường dẫn đến file Shape.
            icon (QIcon): Icon của Shape.
        """
        def __init__(self, shape_index: int, image_path: str):
            self.shape_index = shape_index
            self.image_path = image_path
            self.icon = QIcon(image_path)

    class Shapes(list[Shape]):
        """
        Lớp chứa danh sách các Shape được lấy từ file PPTX mẫu.

        Methods:
            add(shape_index: int, image_path: str): Thêm hình ảnh vào danh sách.
        """
        def __init__(self):
            super().__init__()

        def add(self, shape_index: int, image_path: str):
            shape = Input.Shape(shape_index, image_path)
            self.append(shape)

    class Config:
        """
        Lớp chứa cấu hình thay thế.

        Attributes:
            text (List[str]): Danh sách các Text Config.
            image (List[Input.Config.ConfigImage]): Danh sách các Image Config.
        """
        class ConfigImage:
            """
            Lớp chứa Image Config.

            Attributes:
                placeholder (str): Placeholder của hình ảnh.
                shape_index (int): Chỉ số của hình ảnh.
            """
            def __init__(self, placeholder: str, shape_index: int):
                self.placeholder = placeholder
                self.shape_index = shape_index

        def __init__(self):
            self.text: List[str] = []
            self.image: List[Input.Config.ConfigImage] = []

        def add_text(self, text: str):
            """
            Thêm Text Config.

            Args:
                text (str): Text Config.
            """
            self.text.append(text)

        def add_image(self, shape_index: int, placeholder: str):
            """
            Thêm Image Config.

            Args:
                shape_index (int): Chỉ số của hình ảnh.
                placeholder (str): Placeholder của hình ảnh.
            """
            config_image_item = self.ConfigImage(placeholder, shape_index)
            self.image.append(config_image_item)

    def __init__(self):
        self.pptx = self.Path()
        self.csv = self.Csv()
        self.shapes = self.Shapes()
        self.config = self.Config()
        self.save = self.Path()

class PowerPoint:
    """
    Lớp chứa các phương thức thao tác với PowerPoint.

    Attributes:
        instance (win32com.client.CDispatch): Đối tượng PowerPoint.
        presentation: Đối tượng bản trình chiếu PowerPoint.
    """
    def __init__(self):
        self.instance: win32com.client.CDispatch = None
        self.presentation = None

    def open_instance(self):
        """
        Mở PowerPoint.

        Returns:
            win32com.client.CDispatch: Đối tượng PowerPoint.
        """
        # Tạo môi trường COM cho thread
        pythoncom.CoInitialize()

        if not self.instance:
            try:
                self.instance = win32com.client.Dispatch("PowerPoint.Application")
            except Exception:
                self.instance = None
        return self.instance

    def open_presentation(self, path, read_only=False):
        """
        Mở một file trình chiếu PowerPoint.

        Args:
            path (str): Đường dẫn đến file trình chiếu.
            read_only (bool): Mở file ở chế độ chỉ đọc.

        Returns:
            Exception: Ngoại lệ nếu có lỗi xảy ra.
        """
        MSOFALSE = 0
        MSOTRUE = -1

        read_only_in_mso = MSOTRUE if read_only else MSOFALSE
        has_title = MSOFALSE
        window = MSOFALSE
        try:
            if self.instance and not self.presentation:
                # Mở File PowerPoint
                self.presentation = self.instance.Presentations.open(
                    path, read_only_in_mso, has_title, window
                )

                # Nếu file mở ở chế độ đọc-ghi, và đang ở Final State
                if not read_only and self.presentation.Final:
                    self.presentation.Final = False
        except Exception as e:
            return e

    #* Không cần đóng instance, vì
    # - Nếu đang mở một file pptx khác thì đóng instance sẽ đóng cả file đó
    # - instance sẽ tự đóng khi không có presentation nào đang được mở  

    def free_com_environment(self):
        """
        Giải phóng môi trường COM.
        """

        #* Giải phóng môi trường COM
        pythoncom.CoUninitialize()
        
    def close_presentation(self, save_before_close: bool = False):
        """
        Đóng file trình chiếu PowerPoint.

        Args:
            save_before_close (bool): True nếu muốn lưu trước khi đóng.

        Returns:
            Exception: Ngoại lệ nếu có lỗi xảy ra.
        """
        try:
            if self.presentation and self.instance:
                # Lưu presentation lại nếu có yêu cầu
                if save_before_close:
                    self.presentation.Save()

                # Đóng presentation
                self.presentation.Close()
        except Exception as e:
            return e

class ProgressLogLevel:
    """
    Lớp chứa các cấp độ log cho Progress.

    Attributes:
        INFO (int): Cấp độ INFO.
        ERROR (int): Cấp độ ERROR.
    """
    INFO = 1
    ERROR = 2
    # Just random number

    @staticmethod
    def get_level_name(level: int) -> str:
        """
        Trả về tên cấp độ log.

        Args:
            level (int): Giá trị cấp độ cần chuyển đổi.

        Returns:
            str: Tên cấp độ tương ứng ("INFO", "ERROR" hoặc "UNKNOWN").
        """
        match level:
            case ProgressLogLevel.INFO:
                return "INFO"
            case ProgressLogLevel.ERROR:
                return "ERROR"
            case _:
                return "UNKNOWN"
