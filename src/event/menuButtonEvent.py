from PyQt5.QtWidgets import QFileDialog, QLineEdit, QPushButton, QWidget
from src.ui import csvView
import csv, os

#? Hàm phụ trợ cho việc xử lý sự kiện của các button
def setPathText(file_path: str, inputLine: QLineEdit):
    # @params: file_path: str, inputLine: QLineEdit
    if file_path:
        # Gán đường dẫn vào QLineEdit
        inputLine.setText(file_path)

def get_csv_fields(file_path: str) -> list[str]:
    # @params: file_path: str
    with open(file_path, newline='', encoding='utf-8') as csvfile:
        reader = csv.DictReader(csvfile)
        return reader.fieldnames if reader.fieldnames else []

def openFolder(path: str):
    # @params: path: str
    folder_path = os.path.abspath(path)
    if not os.path.isdir(folder_path):
        os.makedirs(folder_path)
    os.startfile(folder_path)

#? Riêng với start_button, sau khi các thông tin trong dssv_path, save_path, template_path đã được điền đầy đủ, ta sẽ enable nó
# Kiểm tra với mỗi lần nhập liệu, nếu cả 3 trường đều đã được điền, thì enable start_button
def checkStartButton(ui: QWidget):
    # @params: ui: QWidget
    dssv_path: QLineEdit = ui.dssv_path
    save_path: QLineEdit = ui.save_path
    template_path: QLineEdit = ui.template_path
    start_button: QPushButton = ui.start_button

    if dssv_path.text() and save_path.text() and template_path.text():
        start_button.setEnabled(True)
    else:
        start_button.setEnabled(False)

#? Các hàm xử lý sự kiện của các button
def template_powerpoint_broswe(widget: QWidget, inputLine: QLineEdit):
    # @params: widget: QWidget, inputLine: QLineEdit
    file_path, _ = QFileDialog.getOpenFileName(widget, "Chọn file Template", "", "PowerPoint File (*.pptx)")
    # Load ImageShape here
    setPathText(file_path, inputLine)

def dssv_broswe(placeholderButton: QPushButton, widget: QWidget, inputLine: QLineEdit):
    # @params: placeholderButton: QPushButton, widget: QWidget, inputLine: QLineEdit
    file_path, _ = QFileDialog.getOpenFileName(widget, "Chọn file DSSV", "", "CSV File (*.csv)")
    setPathText(file_path, inputLine)
    placeholderButton.setEnabled(True)

def save_broswe(widget: QWidget, inputLine: QLineEdit):
    # @params: widget: QWidget, inputLine: QLineEdit
    file_path, _ = QFileDialog.getSaveFileName(widget, "Chọn vị trí lưu", "", "PowerPoint File (*.pptx)")
    setPathText(file_path, inputLine)

def viewPlaceholder(inputPath: QLineEdit):
    # @params: inputPath: QLineEdit
    if not inputPath.text():
        popup = csvView.Ui([])
    else:
        popup = csvView.Ui(get_csv_fields(inputPath.text()))
    popup.exec_()

def viewShape():
    shapeFolder = "./images/template"
    openFolder(shapeFolder)