from PyQt5.QtWidgets import QLabel
from translations import format_text

def set_pptx_loaded_label(label: QLabel, num: int = 0):
    """
    Đặt nhãn thông báo số lượng shapes đã tải từ file PPTX.

    Args:
        label (QLabel): Nhãn cần hiển thị.
        num (int): Số lượng shapes đã tải. Mặc định là 0.
    """
    if num == 0:
        label.setVisible(False)
        return
    text = format_text("menu.window.pptx.loaded", shapes=num)
    label.setText(text)
    label.setVisible(True)

def set_csv_loaded_label(label: QLabel, fields: int = 0, students: int = 0):
    """
    Đặt nhãn thông báo số lượng fields và students đã tải từ file CSV.

    Args:
        label (QLabel): Nhãn cần hiển thị.
        fields (int): Số lượng fields đã tải. Mặc định là 0.
        students (int): Số lượng students đã tải. Mặc định là 0.
    """
    if fields == 0 or students == 0:
        label.setVisible(False)
        return
    text = format_text("menu.window.csv.loaded", fields=fields, students=students)
    label.setText(text)
    label.setVisible(True)
