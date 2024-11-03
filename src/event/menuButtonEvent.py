from PyQt5.QtWidgets import QFileDialog
from src.ui import csvView
import csv, os

def setPathText(file_path, inputLine):
    if file_path:
        # Gán đường dẫn vào QLineEdit
        inputLine.setText(file_path)
def get_csv_fields(file_path):
    with open(file_path, newline='', encoding='utf-8') as csvfile:
        reader = csv.DictReader(csvfile)
        return reader.fieldnames if reader.fieldnames else []
def openFolder(path):
    folder_path = os.path.abspath(path)
    if not os.path.isdir(folder_path):
        os.makedirs(folder_path)
    os.startfile(folder_path)

def template_powerpoint_broswe(widget, inputLine):
    file_path, _ = QFileDialog.getOpenFileName(widget, "Chọn file Template", "", "PowerPoint File (*.pptx)")
    setPathText(file_path, inputLine)

def dssv_broswe(placeholderButton, widget, inputLine):
    file_path, _ = QFileDialog.getOpenFileName(widget, "Chọn file DSSV", "", "CSV File (*.csv)")
    setPathText(file_path, inputLine)

def save_broswe(widget, inputLine):
    file_path, _ = QFileDialog.getSaveFileName(widget, "Chọn vị trí lưu", "", "PowerPoint File (*.pptx)")
    setPathText(file_path, inputLine)
def viewPlaceholder(inputPath):
    if not inputPath.text():
        popup = csvView.Ui([])
    else:
        popup = csvView.Ui(get_csv_fields(inputPath.text()))
    popup.exec_()
def viewShape():
    shapeFolder = "./image/template"
    openFolder(shapeFolder)
