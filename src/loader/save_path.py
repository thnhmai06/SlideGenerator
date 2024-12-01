from PyQt5.QtWidgets import QLineEdit
from globals import user_input

def get_save_path(line_widget: QLineEdit) -> str:
    save_path = line_widget.text()
    user_input.save.setPath(save_path)
    return save_path