from PyQt5.QtWidgets import QTableWidget, QComboBox, QTableWidgetItem
from src.logging.info import default as info
from src.logging.debug import console_debug
from globals import user_input, SHAPES_PATH
from typing import Tuple
import os

def __sync_item_with_combo(item: QTableWidgetItem, combo: QComboBox):
    item.setText(combo.currentText())

def shapes_viewShapes():
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
    def placeholder_item_setup(row: int) -> Tuple[QTableWidgetItem, QComboBox]:
        item = QTableWidgetItem()
        combo = QComboBox(config_image_table)
        combo.addItems(user_input.csv.placeholders)
        combo.currentTextChanged.connect(
            lambda: __sync_item_with_combo(item, combo)
        )  # Khi chọn item khác, cập nhật nội dung item
        combo.setStyleSheet("background: white; border: none;")
        combo.setCurrentIndex(row)
        __sync_item_with_combo(item, combo) #for sure because first item already has first index, so combo.currentTextChanged will not be triggered
        return item, combo
    
    def shape_item_setup(row: int) -> Tuple[QTableWidgetItem, QComboBox]:
        item = QTableWidgetItem()
        combo = QComboBox(config_image_table)
        combo.currentTextChanged.connect(
            lambda: __sync_item_with_combo(item, combo)
        )  # Khi chọn item khác, cập nhật nội dung item
        for shape in user_input.shapes:
            combo.addItem(shape.icon, str(shape.shape_id))
            combo.setStyleSheet("background: white; border: none;")
        combo.setCurrentIndex(row)
        return item, combo
    
    # Tìm dòng mới
    row = config_image_table.rowCount()

    # Nếu số lượng item vượt quá len của placeholder hoặc shapes, không thêm item mới mà thông báo
    if row >= len(user_input.csv.placeholders):
        info(__name__, "config.too_much_placeholders")
        return
    if row >= len(user_input.shapes):
        info(__name__, "config.too_much_shapes")
        return

    # Tạo item mới
    config_image_table.insertRow(row)
    placeholder_item, placeholder_combo = placeholder_item_setup(row) # Setup cột đầu tiên (placeholder) của item
    shape_item, shape_combo = shape_item_setup(row) # Setup cột thứ hai (shape) của item

    console_debug(__name__, None, f"Add image item {row + 1}")
    # Thêm item mới
    config_image_table.setItem(row, 0, shape_item)
    config_image_table.setItem(row, 1, placeholder_item)
    # Gán combo box vào item
    config_image_table.setCellWidget(row, 0, shape_combo)
    config_image_table.setCellWidget(row, 1, placeholder_combo)
