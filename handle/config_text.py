from PyQt5.QtWidgets import QListWidget, QListWidgetItem, QWidget, QComboBox
from logger.info import default as info
from globals import csv_file
from translations import TRANS

__placeholders = csv_file.placeholders

def remove_item(config_text_list: QListWidget):
    # Lấy các item được chọn
    selected_items: list[QListWidgetItem] = config_text_list.selectedItems()
    
    if selected_items:
        # Xóa item được chọn
        for item in selected_items:
            row = config_text_list.row(item)
            config_text_list.takeItem(row)
    else:
        # Nếu không có item nào được chọn, xóa item cuối cùng
        if config_text_list.count() > 0:
            last_row = config_text_list.count() - 1
            config_text_list.takeItem(last_row)

def add_item(config_text_list: QListWidget):
    # Nếu số lượng item vượt quá placeholders, không thêm item mới mà thông báo
    if config_text_list.count() >= len(__placeholders):
        info(__name__, "too_much_placeholders")
        return

    # Thêm item mới
    item = QListWidgetItem()
    combo = QComboBox(config_text_list)
    combo.addItems(__placeholders)
    combo.setCurrentText(__placeholders[0])
    combo.setStyleSheet("background: white; border: none; QComboBox::drop-down { border: none; background: white; }")
    
    # Thêm item mới
    config_text_list.addItem(item)
    config_text_list.setItemWidget(item, combo)