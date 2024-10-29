from PyQt5.QtWidgets import QFileDialog
import os

def setPathText(widget, file_path, inputLine):
    if file_path:
        # Gán đường dẫn vào QLineEdit
        inputLine.setText(file_path)

def template_powerpoint_broswe(widget, inputLine):
    file_path, _ = QFileDialog.getOpenFileName(widget, "Chọn file Template", "", "PowerPoint File (*.pptx)")
    setPathText(widget, file_path, inputLine)

def dssv_broswe(widget, inputLine):
    file_path, _ = QFileDialog.getOpenFileName(widget, "Chọn file DSSV", "", "CSV File (*.csv)")
    setPathText(widget, file_path, inputLine)

def save_broswe(widget, inputLine):
    file_path, _ = QFileDialog.getSaveFileName(widget, "Chọn vị trí lưu", "", "PowerPoint File (*.pptx)")
    setPathText(widget, file_path, inputLine)

def openLogFolder():
    folder_path = os.path.abspath("./log")
    if not os.path.isdir(folder_path):
        os.makedirs(folder_path)
    os.startfile(folder_path)