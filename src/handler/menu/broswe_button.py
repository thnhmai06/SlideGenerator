from PyQt5.QtWidgets import QFileDialog, QLineEdit, QWidget
from src.loader.shapes import process_shapes
from src.loader.csv import process_csv
from src.loader.save_path import get_save_path
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu


# ? Hàm phụ trợ cho việc xử lý sự kiện của các button
def __setPathText(file_path: str, inputLine: QLineEdit):
    # @params: file_path: str, inputLine: QLineEdit
    if file_path:
        # Gán đường dẫn vào QLineEdit
        inputLine.setText(file_path)


# ? Các hàm xử lý sự kiện của các button
def pptx_broswe(menu: "Menu"):
    # @params: menu: "Menu"
    widget = menu.centralwidget
    inputLine = menu.pptx_path
    file_path, _ = QFileDialog.getOpenFileName(
        widget, "Chọn file Template", "", "PowerPoint File (*.pptx)"
    )
    # Load ImageShape here
    __setPathText(file_path, inputLine)
    if file_path:
        process_shapes(menu)


def csv_broswe(menu: "Menu"):
    # @params: menu: "Menu"
    widget = menu.centralwidget
    inputLine = menu.csv_path
    file_path, _ = QFileDialog.getOpenFileName(
        widget, "Chọn file DSSV", "", "CSV File (*.csv)"
    )
    __setPathText(file_path, inputLine)
    if file_path:
        process_csv(menu)


def save_path_broswe(widget: QWidget, inputLine: QLineEdit):
    # @params: widget: QWidget, inputLine: QLineEdit
    file_path, _ = QFileDialog.getSaveFileName(
        widget, "Chọn vị trí lưu", "", "PowerPoint File (*.pptx)"
    )
    __setPathText(file_path, inputLine)
    if file_path:
        get_save_path(inputLine)
