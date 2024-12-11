import os
from typing import TYPE_CHECKING
from PyQt5.QtWidgets import QLineEdit
from PyQt5.QtCore import pyqtSignal, QObject
from classes.models import PowerPoint
from src.utils.file import delete_file
from src.logging.info import default as info, console_info
from src.logging.error import default as error
from src.utils.ui.controls import clear_config_image_table, toggle_config_image
from globals import user_input, SHAPES_PATH
from translations import TRANS

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

def get_pptx_path(line_widget: QLineEdit) -> str:
    import os

    pptx_path = line_widget.text()
    user_input.pptx.setPath(os.path.abspath(pptx_path))
    return pptx_path

def __logging_no_slide(menu: "Menu"):
    menu.pptx_path.clear()  # Xóa đường dẫn file pptx
    info(__name__, "pptx.no_slide")

def __logging_too_much_slide(menu: "Menu"):
    menu.pptx_path.clear()  # Xóa đường dẫn file pptx
    info(__name__, "pptx.too_much_slides")

def __logging_can_not_open(menu: "Menu", e: Exception):
    menu.pptx_path.clear()  # Xóa đường dẫn file pptx
    error(__name__, "pptx.can_not_open", str(e))

def __toogle_browse_button(menu: "Menu", is_enable: bool):
    menu.pptx_broswe.setEnabled(is_enable)

class GetShapesWorker(QObject):
    logging_no_slide = pyqtSignal()
    logging_too_much_slide = pyqtSignal()
    toggle_config_image = pyqtSignal(bool)
    menu_config_image_clearContents = pyqtSignal()
    menu_config_image_viewShapes_setEnabled = pyqtSignal(bool)
    can_not_open = pyqtSignal(Exception)
    toogle_browse_button = pyqtSignal(bool)
    onFinished = pyqtSignal()

    def __init__(self, powerpoint: PowerPoint):
        super().__init__()
        self.powerpoint = powerpoint

    def quit(self):
        self.powerpoint.close_presentation()
        self.toogle_browse_button.emit(True)  # Enable browse button
        self.onFinished.emit()

    def _get_shapes(self, slide_index=1):
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
            return self.quit()

        self._get_shapes()

        # Enable viewShapes button
        self.menu_config_image_viewShapes_setEnabled.emit(True)

        # Chỉ khi đã có sẵn placeholder rồi thì mới enable config_image_table
        if user_input.csv.number_of_students > 0:
            self.toggle_config_image.emit(True)
        return self.quit()


def process_shapes(menu: "Menu"):
    get_pptx_path(menu.pptx_path)
    console_info(__name__, TRANS["console"]["info"]["pptx_load"], user_input.pptx.path)

    # Worker
    thread = menu.get_shapes_thread
    worker = GetShapesWorker(thread.powerpoint)
    
    #? Worker Configuration
    # Các hàm báo cáo file không phù hợp
    worker.logging_no_slide.connect(lambda: __logging_no_slide(menu))
    worker.logging_too_much_slide.connect(lambda: __logging_too_much_slide(menu))
    # Các hàm tương tác UI
    worker.toggle_config_image.connect(lambda is_enable: toggle_config_image(menu, is_enable))
    worker.menu_config_image_clearContents.connect(lambda: clear_config_image_table(menu))
    worker.menu_config_image_viewShapes_setEnabled.connect(menu.config_image_viewShapes.setEnabled)
    worker.can_not_open.connect(lambda expection: __logging_can_not_open(menu, expection))
    worker.toogle_browse_button.connect(lambda is_enable: __toogle_browse_button(menu, is_enable))

    #? Thread Configuration
    thread.toogle_browse_button.connect(worker.toogle_browse_button.emit)
    thread.run = worker.run
    worker.onFinished.connect(thread.quit)

    thread.start()  # Bắt đầu quá trình lấy ảnh từ slide
