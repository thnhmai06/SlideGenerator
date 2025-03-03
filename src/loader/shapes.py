import os
from typing import TYPE_CHECKING, cast
from PyQt5.QtWidgets import QLineEdit
from PyQt5.QtCore import pyqtSignal, QObject
from classes.models import PowerPoint
from classes.threads import PowerPointWorkerThread as LoadShapesThread
from src.logging.info import console_info
from src.utils.file import delete_file
from src.utils.ui.menu.clear import clear_config_image_table
from src.utils.ui.menu.toggle import toggle_pptx_browse_button, toggle_config_image
from src.utils.ui.menu.set import set_pptx_loaded_label
from src.utils.ui.menu import logging as menu_logging
from globals import user_input, SHAPES_PATH
from translations import get_text

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

def get_pptx_path(line_widget: QLineEdit) -> str:
    """
    Lấy đường dẫn file PowerPoint từ UI và cập nhật vào user_input.

    Args:
        line_widget (QLineEdit): Widget chứa đường dẫn file PowerPoint.

    Returns:
        str: Đường dẫn file PowerPoint.
    """
    pptx_path = line_widget.text()
    user_input.pptx.setPath(pptx_path)
    return pptx_path

class LoadShapesWorker(QObject):
    # Tín hiệu xử lý báo cáo file không phù hợp
    logging_no_slide = pyqtSignal()
    logging_too_much_slide = pyqtSignal()
    logging_can_not_open = pyqtSignal(Exception)
    logging_always_read_only = pyqtSignal()
    # Tín hiệu xử lý UI
    toggle_browse_button = pyqtSignal(bool)
    toggle_config_image = pyqtSignal(bool)
    set_loaded_label = pyqtSignal(int)
    menu_config_image_clearContents = pyqtSignal()
    menu_config_image_viewShapes_setEnabled = pyqtSignal(bool)
    # Tín hiệu kết thúc
    finished = pyqtSignal()

    def __init__(self, powerpoint: PowerPoint):
        super().__init__()
        self.powerpoint = powerpoint

    def _get_each_filled_shape(self, index: int, shape):
        """
        Xử lý từng shape trong slide.

        Args:
            index (int): Chỉ số của shape.
            shape: Đối tượng shape trong slide.
        """
        PP_SHAPEFORMATPNG = 2  # https://learn.microsoft.com/en-us/office/vba/api/powerpoint.shape.export#:~:text=Compressed%20JPG-,ppShapeFormatPNG,-2
        MSO_FILLPICTURE = 6  # https://learn.microsoft.com/en-us/office/vba/api/office.msofilltype#:~:text=Patterned%20fill-,msoFillPicture,-6
        MSO_FILLTEXTURED = 4 # https://learn.microsoft.com/en-us/office/vba/api/office.msofilltype#:~:text=Solid%20fill-,msoFillTextured,-4
        
        fill = shape.Fill
        # Nếu Shape được fill bởi 1 hình ảnh
        if fill.Type == MSO_FILLPICTURE or fill.Type == MSO_FILLTEXTURED:
            file_name = f"{index}.png" 
            # Không lưu tên file dưới dạng id nữa, lưu dưới dạng chỉ số index tương ứng trong slide.shapes
            # Vì không có cách nào để truy cập trực tiếp đến shape bằng ID (why Microsoft tạo ra id để làm gì?) 

            # Lưu ảnh
            path = os.path.join(SHAPES_PATH, file_name)
            shape.Export(path, PP_SHAPEFORMATPNG)
            
            # Lưu thông tin ảnh vào bộ nhớ
            user_input.shapes.add(index, path)
            console_info(__name__, f"Export: Shape {index} -> {path}")

    def get_filled_shapes(self, slide_index=1):
        """
        Lấy ảnh từ những shape được fill bởi ảnh này

        * **Lưu ý:** Không phải là lưu shape Picture, mà những shape mà được fill by picture/texture vào

        Args:
            slide_index (int, optional): Index của slide trong Presentation. Defaults to 1.
        Returns:
            None
        """
        # Xóa hết ảnh cũ
        delete_file(SHAPES_PATH)

        # Tạo folder
        os.makedirs(SHAPES_PATH)

        # Truy cập tới slide cần lấy shape
        slide = self.powerpoint.presentation.Slides(slide_index)
        for iteractor, shape in enumerate(slide.Shapes):
            index = iteractor + 1
            self._get_each_filled_shape(index, shape)

    def quit(self):
        """
        Bật lại nút duyệt file, báo hoàn thành.
        """
        self.toggle_browse_button.emit(True)  # Bật lại nút duyệt file
        self.finished.emit()  # Báo đã hoàn thành xong

    def run(self):
        """
        Chạy quá trình lấy shapes từ slide.
        """
        user_input.shapes.clear()  # Clear shapes
        self.set_loaded_label.emit(0)  # Set loaded_label to 0 (hide)
        self.toggle_config_image.emit(False)  # Disable config_image_table
        self.menu_config_image_clearContents.emit()  # Clear config_image_table
        self.menu_config_image_viewShapes_setEnabled.emit(False)  # Disable viewShapes button
        self.toggle_browse_button.emit(False)  # Disable browse button

        MSOTRUE = -1

        # Mở file
        self.powerpoint.open_instance()
        open_status = self.powerpoint.open_presentation(
            path=user_input.pptx.path, 
            read_only=False
        )

        # Nếu không mở được file
        if isinstance(open_status, Exception):
            self.logging_can_not_open.emit(open_status)
            return self.quit()
        
        # Nếu file ở chế độ Luôn mở Chỉ đọc -> Không thể thao tác trên bản sao của file (cho việc replace)
        if self.powerpoint.presentation.ReadOnly == MSOTRUE:
            self.logging_always_read_only.emit()
            return self.quit()

        # Đếm số slide
        slide_count = self.powerpoint.presentation.Slides.Count
        # Nếu không có Slide nào
        if slide_count == 0:
            self.logging_no_slide.emit()
            return self.quit()
        # Nếu nhiều hơn 1 slide
        if slide_count > 1:
            self.logging_too_much_slide.emit()
            return self.quit()
        # -> Slide chỉ có 1 slide

        # Lấy shapes được fill trong slide
        self.get_filled_shapes(slide_index=1)

        # Set loaded_label
        self.set_loaded_label.emit(len(user_input.shapes)) 
        # Enable viewShapes button
        self.menu_config_image_viewShapes_setEnabled.emit(True)
        # Chỉ khi đã có sẵn placeholder rồi thì mới enable config_image_table
        if user_input.csv.number_of_students > 0:
            self.toggle_config_image.emit(True)

        self.quit()

def load_shapes(menu: "Menu"):
    """
    Xử lý shapes từ slide và cập nhật giao diện người dùng.

    Args:
        menu (Menu): Đối tượng Menu chứa các widget giao diện người dùng.
    """
    # Lấy đường dẫn file PowerPoint
    pptx_path = get_pptx_path(menu.pptx_path)
    console_info(__name__, get_text("console.info.pptx_load"), pptx_path)

    #? Khai báo Worker
    menu.load_shapes_thread = cast(type(LoadShapesThread), LoadShapesThread()) # type: ignore
    assert isinstance(menu.load_shapes_thread, LoadShapesThread)
    menu.load_shapes_worker = cast(type(LoadShapesWorker), LoadShapesWorker(menu.load_shapes_thread.powerpoint)) # type: ignore 
    assert isinstance(menu.load_shapes_worker, LoadShapesWorker)
    # https://stackoverflow.com/questions/67704387/vscode-using-nonlocal-causes-variable-type-never

    #? Bàn giao Worker cho Thread
    menu.load_shapes_worker.moveToThread(menu.load_shapes_thread)  # Di chuyển worker vào thread
    menu.load_shapes_thread.started.connect(menu.load_shapes_worker.run)  # Kết nối signal bắt đầu của thread với hàm run của worker

    #? Nối tín hiệu
    # - Kết nối các hàm báo cáo file không phù hợp với signal
    menu.load_shapes_worker.logging_no_slide.connect(lambda: menu_logging.no_slide(menu.pptx_path))
    menu.load_shapes_worker.logging_too_much_slide.connect(lambda: menu_logging.too_much_slide(menu.pptx_path))
    menu.load_shapes_worker.logging_can_not_open.connect(lambda exception: menu_logging.can_not_open(menu.pptx_path, exception))
    menu.load_shapes_worker.logging_always_read_only.connect(lambda: menu_logging.always_read_only(menu.pptx_path))
    # - Kết nối các hàm xử lý UI với signal
    menu.load_shapes_worker.toggle_browse_button.connect(lambda is_enable: toggle_pptx_browse_button(menu.pptx_browse, is_enable))
    menu.load_shapes_worker.toggle_config_image.connect(lambda is_enable: toggle_config_image(menu, is_enable))
    menu.load_shapes_worker.set_loaded_label.connect(lambda num: set_pptx_loaded_label(menu.pptx_loaded, num))
    menu.load_shapes_worker.menu_config_image_clearContents.connect(lambda: clear_config_image_table(menu.config_image_table))
    menu.load_shapes_worker.menu_config_image_viewShapes_setEnabled.connect(menu.config_image_viewShapes.setEnabled)
    # - Khi worker kết thúc
    menu.load_shapes_worker.finished.connect(menu.load_shapes_thread.quit)  # Kết nối signal báo kết thúc của worker với hàm kết thúc của thread
    menu.load_shapes_worker.finished.connect(menu.load_shapes_worker.deleteLater)  # Xóa worker khi kết thúc
    menu.load_shapes_thread.finished.connect(menu.load_shapes_thread.deleteLater)  # Xóa thread khi kết thúc

    # Bắt đầu quá trình lấy ảnh từ slide
    menu.load_shapes_thread.start()