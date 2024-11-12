from PyQt5.QtWidgets import QLineEdit, QListWidget, QPushButton, QWidget
from typing import TYPE_CHECKING
from logger.info import console_info, default as info
from logger.debug import console_debug
from globals import csv_file
from handle import config_text

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui
    
def __toggle_config_text(ui: 'Ui', is_enable: bool):
    # Enable the config_text_list, add_button, and remove_button
    config_text_list = ui.config_text_list
    add_button = ui.config_text_add_button
    remove_button = ui.config_text_remove_button

    config_text_list.setEnabled(is_enable)
    add_button.setEnabled(is_enable)
    remove_button.setEnabled(is_enable)

def load(ui: 'Ui'):
    csv_path = ui.csv_path.text()
    config_text_list = ui.config_text_list
    config_image_table = ui.config_image_table

    if not csv_path:
        return
    __toggle_config_text(ui, False)
    # Clear config_text_list và config_image_table
    config_text_list.clear()
    config_image_table.clear()

    console_info(__name__, "CSV Path:", csv_path)
    csv_file.load(csv_path) #Chuyển dữ liệu thô từ file csv vào csv_file (ở globals)
    is_csv_vaild = csv_file.get() # Xử lý dữ liệu thô trong csv_file và Chuyển dữ liệu vào dict (ở globals) 
    if not is_csv_vaild:
        info(__name__, "invaild_csv")
        return
    console_info(__name__, "Fields:", (" - ").join(csv_file.placeholders))
    console_info(__name__, "Students:", f"({len(csv_file.students)})")

    __toggle_config_text(ui, True)