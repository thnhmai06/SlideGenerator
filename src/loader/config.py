from PyQt5.QtWidgets import QListWidget, QTableWidget
from globals import user_input

def _load_text_config(config_text: QListWidget):
    """
    Lấy Text Config từ widget và cập nhật vào user_input.

    Args:
        config_text (QListWidget): Widget chứa Text Config.
    """
    user_input.config.text.clear()  # Xóa text config được lưu trước đó
    for index in range(config_text.count()):
        item = config_text.item(index)
        user_input.config.add_text(item.text())

def _load_image_config(config_image: QTableWidget):
    """
    Lấy Image Config từ widget và cập nhật vào user_input.

    Args:
        config_image (QTableWidget): Widget chứa Image Config.
    """
    user_input.config.images.clear()  # Xóa image config cũ được lưu trước đó
    for row in range(config_image.rowCount()):
        shape_index = int(config_image.item(row, 0).text())
        placeholder = config_image.item(row, 1).text()
        user_input.config.add_image(shape_index, placeholder)

def load_config(config_text: QListWidget, config_image: QTableWidget):
    """
    Lấy Config từ các widget và cập nhật vào user_input.

    Args:
        config_text (QListWidget): Widget chứa Text Config.
        config_image (QTableWidget): Widget chứa Image Config.
    """
    _load_text_config(config_text)
    _load_image_config(config_image)