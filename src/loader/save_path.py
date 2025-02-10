from PyQt5.QtWidgets import QLineEdit
from src.logging.info import console_info
from translations import TRANS
from globals import user_input

def load_save_path(line_widget: QLineEdit) -> str:
    """
    Lấy đường dẫn lưu từ QLineEdit và cập nhật vào user_input.

    Args:
        line_widget (QLineEdit): Widget chứa đường dẫn lưu.

    Returns:
        str: Đường dẫn lưu.
    """
    save_path = line_widget.text()
    console_info(__name__, TRANS["console"]["info"]["save_load"], save_path)
    user_input.save.setPath(save_path)
    return save_path