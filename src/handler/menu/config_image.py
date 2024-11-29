from PyQt5.QtWidgets import QTableWidget, QComboBox, QTableWidgetItem
from src.logger.info import default as info
from src.logger.debug import console_debug
from globals import input, SHAPES_PATH
from typing import Tuple
import os


def __refresh_placeholders():
    # Làm mới placeholders ở local file này
    global __placeholders
    __placeholders = input.csv.placeholders


def __refresh_shapes():
    # Làm mới placeholders ở local file này
    global __shapes
    __shapes = input.shapes


def __sync_item_with_combo(item: QTableWidgetItem, combo: QComboBox):
    combo.setCurrentText(item.text())


def preview():
    if not os.path.exists(SHAPES_PATH):
        os.makedirs(SHAPES_PATH)
    if os.name == "nt":  # Check if the OS is Windows
        os.startfile(SHAPES_PATH)


def remove_item(config_image_table: QTableWidget):
    selected_item = config_image_table.currentItem()
    if selected_item:
        # Xóa item đang chọn
        row = config_image_table.row(selected_item)
        console_debug(
            __name__, None, f"Remove image item {row + 1}"
        )  # row + 1 vì row bắt đầu từ 0
        config_image_table.removeRow(row)
    else:
        if config_image_table.rowCount() > 0:
            last_row = config_image_table.rowCount()
            console_debug(__name__, None, f"Remove image item {last_row}")
            config_image_table.removeRow(last_row - 1)
        else:
            info(__name__, "config.no_item_to_remove")


def add_item(config_image_table: QTableWidget):
    def text_item_setup(row_count: int) -> Tuple[QTableWidgetItem, QComboBox]:
        item_placeholder = QTableWidgetItem()
        combo_placeholder = QComboBox(config_image_table)
        combo_placeholder.addItems(__placeholders)
        combo_placeholder.currentTextChanged.connect(
            lambda: __sync_item_with_combo(item_placeholder, combo_placeholder)
        )  # Khi chọn item khác, cập nhật item text
        combo_placeholder.setCurrentIndex(row_count)
        combo_placeholder.setStyleSheet("background: white; border: none;")
        return item_placeholder, combo_placeholder
    
    def image_item_setup() -> Tuple[QTableWidgetItem, QComboBox]:
        item_image = QTableWidgetItem()
        combo_image = QComboBox(config_image_table)
        combo_image.currentTextChanged.connect(
            lambda: __sync_item_with_combo(item_image, combo_image)
        )  # Khi chọn item khác, cập nhật item text
        for combobox_item in input.shapes:
            combo_image.addItem(combobox_item["icon"], f"ID {combobox_item['id']}")
            combo_image.setStyleSheet("background: white; border: none;")
        combo_image.setCurrentIndex(row_count)
        return item_image, combo_image

    __refresh_placeholders()
    __refresh_shapes()
    # Tìm dòng mới
    row_count = config_image_table.rowCount()

    # Nếu số lượng item vượt quá len của placeholder hoặc shapes, không thêm item mới mà thông báo
    if row_count >= len(__placeholders):
        info(__name__, "config.too_much_placeholders")
        return
    if row_count >= len(__shapes):
        info(__name__, "config.too_much_shapes")
        return

    # Tạo item mới
    config_image_table.insertRow(row_count)
    # Setup cột đầu tiên (placeholder) của item
    _item_placeholder, _combo_placeholder = text_item_setup(row_count)
    # Setup cột thứ hai (image) của item
    _item_image, _combo_image = image_item_setup()

    # Thêm item mới
    console_debug(__name__, None, f"Add image item {row_count + 1}")
    config_image_table.setItem(row_count, 0, _item_image)
    config_image_table.setItem(row_count, 1, _item_placeholder)
    # Gán combo box vào item
    config_image_table.setCellWidget(row_count, 0, _combo_image)
    config_image_table.setCellWidget(row_count, 1, _combo_placeholder)
