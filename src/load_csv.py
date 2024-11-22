from PyQt5.QtWidgets import QLineEdit, QListWidget, QPushButton, QWidget
from typing import TYPE_CHECKING
from logger.info import console_info, default as info
from globals import Input
from src.toggle_config import toggle_config_text, toggle_config_image

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui

def __refresh_shapes():
    # Làm mới placeholders ở local file này
    global __shapes
    __shapes = Input.shapes

def load(ui: 'Ui'):
    csv_path = ui.csv_path.text()
    config_text_list = ui.config_text_list
    config_image_table = ui.config_image_table

    if not csv_path:
        return
    toggle_config_text(ui, False)

    # Clear config_text_list và config_image_table
    config_text_list.clear()
    config_image_table.clearContents()

    console_info(__name__, "CSV Path:", csv_path)
    Input.csv.read_csv(csv_path) #Chuyển dữ liệu thô từ file csv vào Input.csv (ở globals)
    is_Csv_vaild = Input.csv.get() # Xử lý dữ liệu thô trong Input.csv và Chuyển dữ liệu vào dict (ở globals) 
    if not is_Csv_vaild:
        info(__name__, "invaild_csv")
        return
    console_info(__name__, "Fields:", (" - ").join(Input.csv.placeholders))
    console_info(__name__, "Students:", f"({len(Input.csv.students)})")

    __refresh_shapes()
    toggle_config_text(ui, True)
    # Nếu có Shapes ảnh thì enable config_image
    if (len(__shapes) > 0):
        toggle_config_image(ui, True)