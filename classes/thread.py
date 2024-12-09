import sys
import os
from PyQt5.QtCore import QThread, pyqtSignal
from classes.models import PowerPoint
from src.logging.debug import console_debug
from src.logging.info import default as info, console_info
from src.utils.file import delete_file
from globals import SHAPES_PATH, user_input
from typing import Callable


def _onFinished(powerpoint: PowerPoint):
    if powerpoint.presentation:
        powerpoint.close_presentation()
    if powerpoint.instance:
        powerpoint.close_instance()


class CheckingThread(QThread):
    # ? Logging
    logging_no_powerpoint = pyqtSignal()

    def __init__(self):
        super().__init__()
        self.powerpoint = PowerPoint()

        # ? Logging Connection
        self.logging_no_powerpoint.connect(
            lambda: {info(__name__, "no_powerpoint"), sys.exit(1)}
        )

    def quit(self):
        _onFinished(self.powerpoint)
        self.exit()

    def run(self):
        try:
            # Thử tạo giao thức với PowerPoint
            self.powerpoint.open_instance()
        except Exception as e:
            # Không có Powerpoint
            console_debug(__name__, None, str(e))
            self.logging_no_powerpoint.emit()
            # Ở đây không close Powerpoint vì có mở Powerpoint được đâu
        else:
            # Có Powerpoint
            console_debug(__name__, "powerpoint_found")
        finally:
            self.quit()


class GetShapesThread(QThread):
    logging_no_slide = pyqtSignal()
    logging_too_much_slide = pyqtSignal()
    toggle_config_image = pyqtSignal(bool)
    menu_config_image_clearContents = pyqtSignal()
    menu_config_image_viewShapes_setEnabled = pyqtSignal(bool)
    can_not_open = pyqtSignal(Exception)
    toogle_browse_button = pyqtSignal(bool)

    def __init__(self):
        super().__init__()
        self.powerpoint = PowerPoint()

    def quit(self):
        self.powerpoint.close_presentation()
        self.toogle_browse_button.emit(True)  # Enable browse button
        self.exit()  # Kết thúc Thread
        self.__init__()

    def __get_shapes(self, slide_index=1):
        """
        Lấy ảnh từ những shape được fill bởi ảnh này
        *Lưu ý: Không phải là lưu shape Picture, mà những shape hình dạng mà được Fill thêm ảnh vào

        Args:
            slide_index (int, optional): Index của slide trong Presentation. Defaults to 1.
        Returns:
            None
        """

        # Tạo folder nếu thư mục lưu không tồn tại
        if not os.path.exists(SHAPES_PATH):
            os.makedirs(SHAPES_PATH)

        # Xóa hết các file trong save_path
        delete_file(SHAPES_PATH)

        MSO_FILLPICTURE = 6  # https://learn.microsoft.com/en-us/office/vba/api/office.msofilltype
        PP_SHAPEFORMATPNG = 2  # https://learn.microsoft.com/en-us/office/vba/api/powerpoint.shape.export

        slide = self.powerpoint.presentation.Slides(slide_index)
        for iteractor, shape in enumerate(slide.Shapes):
            index = iteractor + 1
            fill = shape.Fill
            # Nếu Shape được fill bởi 1 hình ảnh
            if fill.Type == MSO_FILLPICTURE:
                file_name = f"{index}.png" 
                # Không lưu dưới dạng id nữa, lưu dưới dạng chỉ số index tương ứng trong slide.shapes
                # Vì không có cách nào để truy cập trực tiếp đến shape bằng ID (why Microsoft tạo ra id để làm gì?) 
                path = os.path.join(SHAPES_PATH, file_name)

                # Lưu ảnh
                shape.Export(path, PP_SHAPEFORMATPNG)
                # Thêm ảnh vào user_input.config
                user_input.shapes.add(index, path)

                console_info(__name__, f"Export: Shape ID {index} -> {file_name}")

    def run(self):
        user_input.shapes.clear()  # Clear shapes
        self.toggle_config_image.emit(False)  # Disable config_image_table
        self.menu_config_image_clearContents.emit()  # Clear config_image_table
        self.menu_config_image_viewShapes_setEnabled.emit(False)  # Disable viewShapes button
        self.toogle_browse_button.emit(False)  # Disable browse button

        self.powerpoint.open_instance()
        open_status = self.powerpoint.open_presentation(user_input.pptx.path)

        # Nếu không mở được file
        if isinstance(open_status, Exception):
            self.can_not_open.emit(open_status)
            return self.quit()

        slide_count = self.powerpoint.presentation.Slides.Count
        # Nếu không có Slide nào
        if slide_count == 0:
            self.logging_no_slide.emit()
            return self.quit()
        # Nếu nhiều hơn 1 slide
        if slide_count > 1:
            self.logging_too_much_slide.emit()
            self.quit()

        self.__get_shapes()

        # Enable viewShapes button
        self.menu_config_image_viewShapes_setEnabled.emit(True)

        # Chỉ khi đã có sẵn placeholder rồi thì mới enable config_image_table
        if user_input.csv.number_of_students > 0:
            self.toggle_config_image.emit(True)
        return self.quit()


class WorkingThread(QThread):
    def __init__(self):
        super().__init__()
        self.powerpoint = PowerPoint()

    def quit(self):
        self.exit()  # Kết thúc Thread
        # self.__init__()
