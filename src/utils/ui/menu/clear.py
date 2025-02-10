from typing import TYPE_CHECKING
from PyQt5.QtWidgets import QTableWidget, QListWidget

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

def clear_config_image_table(config_image_table: QTableWidget):
    """
    Xóa nội dung bảng config_image_table.

    Args:
        config_image_table (QTableWidget): Widget Image Config.
    """
    config_image_table.clearContents()
    config_image_table.setRowCount(0)

def clear_config_text_list(config_text_list: QListWidget):
    """
    Xóa nội dung danh sách config_text_list.

    Args:
        config_text_list (QListWidget): Widget Text Config.
    """
    config_text_list.clear()

def clear_config(menu: "Menu"):
    """
    Xóa nội dung cả bảng config_image_table và danh sách config_text_list.

    Args:
        menu (Menu): Widget Menu.
    """
    clear_config_text_list(menu.config_text_list)
    clear_config_image_table(menu.config_image_table)
