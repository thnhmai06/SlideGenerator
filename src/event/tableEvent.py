from PyQt5 import QtWidgets

def remove_item(ui):
    # Lấy item đang chọn
    selected_items = ui.config_image_table.selectedItems()
    
    if selected_items:
        # Xóa item đang chọn
        row = selected_items[0].row()
        ui.config_image_table.removeRow(row)
    else: 
        # Xóa item cuối cùng
        last_row = ui.config_image_table.rowCount()
        last_col = ui.config_image_table.columnCount()
        last_item = ui.config_image_table.item(last_row-1, last_col-1)

        if last_item:
            ui.config_image_table.removeRow(last_item.row())
        

def add_item(ui):
    # Tìm dòng mới
    row_count = ui.config_image_table.rowCount()
    ui.config_image_table.insertRow(row_count)
    # Thêm item vào dòng mới
    for col in range(ui.config_image_table.columnCount()):
        item = QtWidgets.QTableWidgetItem(f'Mục mới {row_count + 1}-{col + 1}')
        ui.config_image_table.setItem(row_count, col, item)