from PyQt5 import QtCore
from PyQt5.QtWidgets import *

def remove_item(ui):
    # Lấy các item được chọn
    selected_items = ui.config_text_list.selectedItems()
    
    if selected_items:
        # Xóa item được chọn
        for item in selected_items:
            row = ui.config_text_list.row(item)
            ui.config_text_list.takeItem(row)
    else:
        # Nếu không có item nào được chọn, xóa item cuối cùng
        if ui.config_text_list.count() > 0:
            last_row = ui.config_text_list.count() - 1
            ui.config_text_list.takeItem(last_row)

def add_item(ui):
    # Lấy số lượng item hiện có để tạo item mới với tên mới
    item_count = ui.config_text_list.count() + 1
    new_item = QListWidgetItem(f'{"{"}item{item_count}{"}"}')
    new_item.setFlags(new_item.flags() | QtCore.Qt.ItemIsEditable)
    
    # Thêm item mới
    ui.config_text_list.addItem(new_item)