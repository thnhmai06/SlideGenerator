from PyQt5.QtWidgets import QTableWidget, QTableWidgetItem, QWidget
from translation import TRANS

def remove_item(config_image_table: QTableWidget):
    # Lấy item đang chọn
    selected_items: list[QTableWidgetItem] = config_image_table.selectedItems()
    
    if selected_items:
        # Xóa item đang chọn
        row = selected_items[0].row()
        config_image_table.removeRow(row)
    else: 
        # Xóa item cuối cùng
        last_row = config_image_table.rowCount()
        last_col = config_image_table.columnCount()
        last_item: QTableWidgetItem = config_image_table.item(last_row-1, last_col-1)

        if last_item:
            config_image_table.removeRow(last_item.row())
        

def add_item(config_image_table: QTableWidget):
    # Tìm dòng mới
    row_count = config_image_table.rowCount()
    config_image_table.insertRow(row_count)
    # Thêm item vào dòng mới
    for col in range(config_image_table.columnCount()):
        item = QTableWidgetItem(f'{TRANS["image_table"]["new_item"]} {row_count + 1}-{col + 1}')
        config_image_table.setItem(row_count, col, item)