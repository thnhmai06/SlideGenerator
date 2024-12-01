from globals import user_input
from PyQt5.QtWidgets import QListWidget, QTableWidget

def _get_text_config(config_text: QListWidget):
    for index in range(config_text.count()):
        item = config_text.item(index)
        user_input.config.add_text(item.text())

def _get_image_config(config_image: QTableWidget):
    for row in range(config_image.rowCount()):
        shape_id = int(config_image.item(row, 0).text())
        placeholder = config_image.item(row, 1).text()
        user_input.config.add_image(shape_id, placeholder)

def get_config(config_text: QListWidget, config_image: QTableWidget):
    _get_text_config(config_text)
    _get_image_config(config_image)