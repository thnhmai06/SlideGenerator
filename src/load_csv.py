from PyQt5.QtWidgets import QLineEdit, QListWidget, QPushButton, QWidget
from typing import TYPE_CHECKING
from logger.info import console_info, default as info
from globals import input
from src.get_input import get_csv
from src.toggle_config import toggle_config_text, toggle_config_image

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui

def __refresh_shapes():
    # Làm mới placeholders ở local file này
    global __shapes
    __shapes = input.shapes

def load(ui: 'Ui'):
    csv_path = ui.csv_path.text()
    config_text_list = ui.config_text_list
    config_image_table = ui.config_image_table

    if not csv_path:
        return
    console_info(__name__, "CSV Path:", csv_path)

    # Clear config_text_list và config_image_table
    toggle_config_text(ui, False)
    config_text_list.clear()
    config_image_table.clearContents()

    is_Csv_vaild = get_csv(csv_path) #Thu thập thông tin trong file csv và Chuyển dữ liệu vào dict (ở globals) 
    if not is_Csv_vaild:
        info(__name__, "invaild_csv")
        return

    toggle_config_text(ui, True)

    # Nếu có Shapes ảnh thì enable config_image
    __refresh_shapes()
    if (len(__shapes) > 0):
        toggle_config_image(ui, True)