from PyQt5.QtWidgets import QListWidget, QListWidgetItem, QWidget, QComboBox
from logger.info import default as info
from logger.debug import console_debug
from globals import Input

def __refresh_placeholders():
    # Làm mới placeholders ở local file này
    global __placeholders
    __placeholders = Input.csv.placeholders

def remove_item(config_text_list: QListWidget):
    # Lấy các item được chọn
    selected_items: list[QListWidgetItem] = config_text_list.selectedItems()
    
    if selected_items:
        # Xóa item được chọn
        for item in selected_items:
            row = config_text_list.row(item)
            console_debug(__name__, None, f"Remove text item {row + 1}: {item.text()}") # row + 1 vì row bắt đầu từ 0
            config_text_list.takeItem(row)
    else:
        # Nếu không có item nào được chọn, xóa item cuối cùng
        if config_text_list.count() > 0:
            last_row = config_text_list.count() - 1
            last_row_item = config_text_list.item(last_row)
            console_debug(__name__, None, f"Remove text item {last_row + 1}: {last_row_item.text()}") # last_row + 1 vì last_row bắt đầu từ 0
            config_text_list.takeItem(last_row)
        else:
            info(__name__, "no_item_to_remove")

def add_item(config_text_list: QListWidget):
    __refresh_placeholders()
    items_count = config_text_list.count()

    # Nếu số lượng item vượt quá placeholders, không thêm item mới mà thông báo
    if config_text_list.count() >= len(__placeholders):
        info(__name__, "too_much_placeholders")
        return

    # Thêm item mới
    item = QListWidgetItem()
    item.setText(__placeholders[0]) # Mặc định là item đầu tiên trong placeholders
    combo = QComboBox(config_text_list)
    combo.addItems(__placeholders)
    combo.setStyleSheet("""
        background: white; 
        border: none;
        font-size: 10pt; 
        padding: 5px;
    """)
    item.setSizeHint(combo.sizeHint())
    combo.currentTextChanged.connect(lambda: item.setText(combo.currentText())) # Khi chọn item khác, cập nhật item text
    
    # Thêm item mới
    console_debug(__name__, None, f"Add text item {items_count + 1}: {item.text()}")
    config_text_list.addItem(item)
    config_text_list.setItemWidget(item, combo)