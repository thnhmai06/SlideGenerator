from PyQt5.QtWidgets import QFileDialog, QLineEdit, QPushButton, QWidget
import os
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui

#? Hàm phụ trợ cho việc xử lý sự kiện của các button
def __setPathText(file_path: str, inputLine: QLineEdit):
    # @params: file_path: str, inputLine: QLineEdit
    if file_path:
        # Gán đường dẫn vào QLineEdit
        inputLine.setText(file_path)

def __openFolder(path: str):
    # @params: path: str
    folder_path = os.path.abspath(path)
    if not os.path.isdir(folder_path):
        os.makedirs(folder_path)
    os.startfile(folder_path)

#? Các hàm xử lý sự kiện của các button
def template_powerpoint_broswe(widget: QWidget, inputLine: QLineEdit):
    # @params: widget: QWidget, inputLine: QLineEdit
    file_path, _ = QFileDialog.getOpenFileName(widget, "Chọn file Template", "", "PowerPoint File (*.pptx)")
    # Load ImageShape here
    __setPathText(file_path, inputLine)

def csv_broswe(widget: QWidget, inputLine: QLineEdit):
    # @params: widget: QWidget, inputLine: QLineEdit
    file_path, _ = QFileDialog.getOpenFileName(widget, "Chọn file DSSV", "", "CSV File (*.csv)")
    __setPathText(file_path, inputLine)

def save_path_broswe(widget: QWidget, inputLine: QLineEdit):
    # @params: widget: QWidget, inputLine: QLineEdit
    file_path, _ = QFileDialog.getSaveFileName(widget, "Chọn vị trí lưu", "", "PowerPoint File (*.pptx)")
    __setPathText(file_path, inputLine)

