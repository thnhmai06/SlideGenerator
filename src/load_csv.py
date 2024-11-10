from PyQt5.QtWidgets import QLineEdit, QListWidget, QPushButton, QWidget
from typing import TYPE_CHECKING
from logger.info import _console_info, default as info
from globals import csv_file
from handle import config_text

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui
    

def load(ui: 'Ui'):
    csv_path = ui.csv_path.text()
    config_text_list = ui.config_text_list
    config_image_table = ui.config_image_table
    add_button = ui.config_text_add_button
    remove_button = ui.config_text_remove_button

    if not csv_path:
        return
    config_text_list.clear()
    config_image_table.clear()

    _console_info(__name__, "Import CSV:", csv_path)
    csv_file.load(csv_path)
    1/0
    is_vaild = csv_file.get()
    if not is_vaild:
        info(__name__, "invaild_csv")
        return
    _console_info(__name__, "Fields:", " | ".join(csv_file.placeholders), "(*end)")
    _console_info(__name__, "Students:", f"({len(csv_file.students)})")

    # Enable the config_text_list, add_button, and remove_button when success
    config_text_list.setEnabled(True)
    add_button.setEnabled(True)
    remove_button.setEnabled(True)