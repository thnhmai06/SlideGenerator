from PyQt5.QtWidgets import QLineEdit
from src.logging.info import console_info
from translations import TRANS
from globals import user_input

def get_save_path(line_widget: QLineEdit) -> str:
    save_path = line_widget.text()
    console_info(__name__, TRANS["console"]["info"]["save_load"], save_path)
    user_input.save.setPath(save_path)
    return save_path