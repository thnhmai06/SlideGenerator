from PyQt5.QtWidgets import QListWidget, QListWidgetItem, QComboBox
from src.logging.info import default as info
from src.logging.debug import console_debug
from globals import user_input

def remove_item(config_text_list: QListWidget):
    """
    Xóa item được chọn khỏi danh sách TextConfig.

    Args:
        config_text_list (QListWidget): Danh sách Text Config.
    """
    # Lấy các item được chọn
    selected_items = config_text_list.selectedItems()

    if selected_items:
        # Xóa item được chọn
        for item in selected_items:
            row = config_text_list.row(item)
            console_debug(
                __name__, None, f"Remove text item {row + 1}: {item.text()}"
            )  # row + 1 vì row bắt đầu từ 0
            config_text_list.takeItem(row)
    else:
        # Nếu không có item nào được chọn, xóa item cuối cùng
        if config_text_list.count() > 0:
            last_row = config_text_list.count() - 1
            last_row_item = config_text_list.item(last_row)
            console_debug(
                __name__,
                None,
                f"Remove text item {last_row + 1}: {last_row_item.text()}",
            )  # last_row + 1 vì last_row bắt đầu từ 0
            config_text_list.takeItem(last_row)
        else:
            # Báo không có item để xóa
            info(__name__, "config.no_item_to_remove")

def add_item(config_text_list: QListWidget):
    """
    Thêm một item mới vào danh sách Text Config.

    Args:
        config_text_list (QListWidget): Danh sách Text Config.
    """
    # Nếu số lượng item vượt quá placeholders, không thêm item mới mà thông báo
    text_config_items_count = config_text_list.count()
    if text_config_items_count >= len(user_input.csv.placeholders):
        info(__name__, "config.too_much_placeholders")
        return

    # Cấu hình combo (hộp thả xuống)
    combo = QComboBox(config_text_list)
    combo.addItems(user_input.csv.placeholders)
    combo.setCurrentIndex(text_config_items_count)
    combo.setStyleSheet("""
        background: white; 
        border: none;
        font-size: 10pt; 
        padding: 5px;
    """)
    # Cấu hình item
    item = QListWidgetItem()
    item.setText(combo.currentText())
    item.setSizeHint(combo.sizeHint())
    # Khi chọn item khác trên combo, cập nhật item text
    combo.currentTextChanged.connect(
        lambda: item.setText(combo.currentText()) 
    )

    # Thêm item vào
    console_debug(__name__, None, f"Add text item {text_config_items_count + 1}: {item.text()}")
    config_text_list.addItem(item)
    config_text_list.setItemWidget(item, combo)
