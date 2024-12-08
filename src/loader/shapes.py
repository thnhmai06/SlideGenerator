from typing import TYPE_CHECKING
from PyQt5.QtWidgets import QLineEdit
from globals import user_input
from translations import TRANS
from src.logging.info import default as info, console_info
from src.logging.error import default as error
from src.utils.ui.controls import clear_config_image_table, toggle_config_image

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

def process_shapes(menu: "Menu"):
    get_pptx_path(menu.pptx_path)
    console_info(__name__, TRANS["console"]["info"]["pptx_load"], user_input.pptx.path)

    # Các hàm báo cáo file không phù hợp
    menu.get_shapes_thread.logging_no_slide.connect(lambda: __logging_no_slide(menu))
    menu.get_shapes_thread.logging_too_much_slide.connect(lambda: __logging_too_much_slide(menu))
    # Các hàm tương tác UI
    menu.get_shapes_thread.toggle_config_image.connect(lambda is_enable: toggle_config_image(menu, is_enable))
    menu.get_shapes_thread.menu_config_image_clearContents.connect(lambda: clear_config_image_table(menu))
    menu.get_shapes_thread.menu_config_image_viewShapes_setEnabled.connect(menu.config_image_viewShapes.setEnabled)
    menu.get_shapes_thread.can_not_open.connect(lambda expection: __logging_can_not_open(menu, expection))
    menu.get_shapes_thread.toogle_browse_button.connect(lambda is_enable: __toogle_browse_button(menu, is_enable))

    menu.get_shapes_thread.start()  # Bắt đầu quá trình lấy ảnh từ slide
