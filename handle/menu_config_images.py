from PyQt5.QtWidgets import QTableWidget, QTableWidgetItem, QComboBox, QTableWidgetItem
from translations import TRANS
from logger.info import default as info
from logger.debug import console_debug
from globals import input

def __refresh_placeholders():
    # Làm mới placeholders ở local file này
    global __placeholders
    __placeholders = input.csv.placeholders
def __refresh_shapes():
    # Làm mới placeholders ở local file này
    global __shapes
    __shapes = input.shapes

def remove_item(config_image_table: QTableWidget):
    selected_item = config_image_table.currentItem()
    if selected_item:
        # Xóa item đang chọn
        row = config_image_table.row(selected_item) 
        console_debug(__name__, None, f"Remove image item {row + 1}") # row + 1 vì row bắt đầu từ 0
        config_image_table.removeRow(row)
    else:
        if config_image_table.rowCount() > 0:
            last_row = config_image_table.rowCount()
            console_debug(__name__, None, f"Remove image item {last_row}")
            config_image_table.removeRow(last_row-1)
        else:
            info(__name__, "no_item_to_remove")

def add_item(config_image_table: QTableWidget):
    __refresh_placeholders()
    __refresh_shapes()
    # Tìm dòng mới
    row_count = config_image_table.rowCount()

    # Nếu số lượng item vượt quá len của placeholder hoặc shapes, không thêm item mới mà thông báo
    if config_image_table.rowCount() >= len(__placeholders):
        info(__name__, "too_much_placeholders")
        return
    if config_image_table.rowCount() >= len(__shapes):
        info(__name__, "too_much_shapes")
        return

    config_image_table.insertRow(row_count)
    # Setup cột đầu tiên (placeholder) của item
    item_placeholder = QTableWidgetItem()
    item_placeholder.setText(__placeholders[0]) # Mặc định là item đầu tiên trong placeholders
    combo_placeholder = QComboBox(config_image_table)
    combo_placeholder.addItems(__placeholders)
    combo_placeholder.setStyleSheet("background: white; border: none;")
    combo_placeholder.currentTextChanged.connect(lambda: item_placeholder.setText(combo_placeholder.currentText())) # Khi chọn item khác, cập nhật item text

    # Setup cột thứ hai (image) của item
    item_image = QTableWidgetItem()
    item_image.setText(input.shapes[0]["id"])
    combo_image = QComboBox(config_image_table)
    for combobox_item in input.shapes:
        combo_image.addItem(combobox_item["icon"], f"ID {combobox_item["id"]}")
        combo_image.setStyleSheet("background: white; border: none;")
    combo_image.currentTextChanged.connect(lambda: item_image.setText(combo_image.currentText())) # Khi chọn item khác, cập nhật item text
        
    # Thêm item mới
    console_debug(__name__, None, f"Add image item {row_count + 1}")
    config_image_table.setItem(row_count, 0, item_image)
    config_image_table.setItem(row_count, 1, item_placeholder)
    # Gán combo box vào item
    config_image_table.setCellWidget(row_count, 0, combo_image)
    config_image_table.setCellWidget(row_count, 1, combo_placeholder)