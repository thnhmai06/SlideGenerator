from PyQt5 import QtCore
from PyQt5.QtWidgets import QListWidget, QListWidgetItem, QWidget

def remove_item(ui: QWidget):
    # @params: ui: QWidget
    config_text_list: QListWidget = ui.config_text_list
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

def add_item(ui: QWidget):
    # @params: ui: QWidget
    config_text_list: QListWidget = ui.config_text_list
    # Lấy số lượng item hiện có để tạo item mới với tên mới
    item_count = config_text_list.count() + 1
    new_item = QListWidgetItem(f'item{item_count}')
    new_item.setFlags(new_item.flags() | QtCore.Qt.ItemIsEditable)
    
    # Thêm item mới
    config_text_list.addItem(new_item)