import os
from PyQt5.QtWidgets import QTableWidget, QComboBox, QTableWidgetItem
from src.logging.info import default as info
from src.logging.debug import console_debug
from globals import user_input, SHAPES_PATH

def open_saved_shapes_folder():
    """
    Mở thư mục chứa các hình đã lưu, đồng thời tạo thư mục nếu chưa tồn tại.
    """
    # Tạo thư mục nếu chưa tồn tại
    if not os.path.exists(SHAPES_PATH):
        os.makedirs(SHAPES_PATH)
    # Mở thư mục
    os.startfile(SHAPES_PATH)

def remove_item(config_image_table: QTableWidget):
    """
    Xóa item đang chọn trong bảng Image Config. Nếu không có item nào được chọn, xóa item cuối cùng.

    Args:
        config_image_table (QTableWidget): Bảng Image Config.
    """
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
    """
    Thêm item mới vào bảng Image Config.

    Args:
        config_image_table (QTableWidget): Bảng Image Config.
    """

    class ImageConfigItem:
        """
        Lớp chứa các phương thức thiết lập item trong bảng Image Config.
        Attributes:
            row (int): Số thứ tự hàng của item.
            placeholder (Placeholder): Chứa thuộc tính item bên cột Placeholder.
            shape (Shape): Chứa thuộc tính item bên cột Shape.
        """
        def __init__(self, row: int):
            self.row = row
            self.placeholder = self.Placeholder(row)
            self.shape = self.Shape(row)

        class Placeholder:
            """
            Lớp chứa thuộc tính item bên cột Placeholder.
            Attributes:
                combo (QComboBox): Hộp thả xuống, chứa các placeholder ở user_input.csv.placeholders.
                item (QTableWidgetItem): Là item, được đồng bộ với giá trị hiện tại được chọn của combo box.
            """

            def __init__(self, row: int):
                # Cấu hình combo box
                self.combo = QComboBox(config_image_table)
                self.combo.addItems(user_input.csv.placeholders)
                self.combo.setStyleSheet("background: white; border: none;")
                self.combo.setCurrentIndex(row) # Set giá trị mặc định cho combo box là giá trị của hàng mới tạo
                # Cấu hình item
                self.item = QTableWidgetItem()
                # Khi chọn item khác trong combo box, cập nhật item
                self.combo.currentTextChanged.connect(
                    lambda: self.item.setText(self.combo.currentText())
                )
                self.item.setText(self.combo.currentText())
                # Vì item khi mới tạo đã có sẵn giá trị là giá trị của mục đầu tiên, 
                # cần cập nhật lại giá trị cho đúng của row

        class Shape:
            """
            Lớp chứa thuộc tính item bên cột Shape.
            Attributes:
                combo (QComboBox): Hộp thả xuống, chứa các shape ở user_input.shapes.
                item (QTableWidgetItem): Là item, được đồng bộ với giá trị hiện tại được chọn của combo box.
            """
            def __init__(self, row: int):
                # Cấu hình combo box
                self.combo = QComboBox(config_image_table)
                for shape in user_input.shapes:
                    self.combo.addItem(shape.icon, str(shape.shape_index))
                self.combo.setStyleSheet("background: white; border: none;")
                self.combo.setCurrentIndex(row)  # Set giá trị mặc định cho combo box là giá trị của hàng mới tạo
                # Cấu hình item
                self.item = QTableWidgetItem()
                # Khi chọn item khác trong combo box, cập nhật item
                self.combo.currentTextChanged.connect(
                    lambda: self.item.setText(self.combo.currentText())
                )
                self.item.setText(self.combo.currentText()) #:L88

    # Tìm vị trí dòng mới (chính là số lượng item hiện tại theo 0-based index)
    row = config_image_table.rowCount()

    # Nếu số lượng item vượt quá len của placeholder hoặc shapes, không thêm item mới mà thông báo
    if row >= len(user_input.csv.placeholders):
        info(__name__, "config.too_much_placeholders")
        return
    if row >= len(user_input.shapes):
        info(__name__, "config.too_much_shapes")
        return

    # Tạo item mới
    console_debug(__name__, None, f"Add image item {row + 1}")
    config_image_table.insertRow(row)
    new_item = ImageConfigItem(row)

    # Set Placeholder
    config_image_table.setItem(row, 1, new_item.placeholder.item)
    config_image_table.setCellWidget(row, 1, new_item.placeholder.combo)

    # Set Shape
    config_image_table.setItem(row, 0, new_item.shape.item)
    config_image_table.setCellWidget(row, 0, new_item.shape.combo)
