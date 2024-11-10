from PyQt5.QtWidgets import QFileDialog, QLineEdit, QPushButton, QWidget
import os

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

#? Riêng với start_button, sau khi các thông tin trong dssv_path, save_path, template_path đã được điền đầy đủ, ta sẽ enable nó
# Kiểm tra với mỗi lần nhập liệu, nếu cả 3 trường đều đã được điền, thì enable start_button
def check_start_button(ui: QWidget):
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
    __setPathText(file_path, inputLine)

def dssv_broswe(widget: QWidget, inputLine: QLineEdit):
    # @params: widget: QWidget, inputLine: QLineEdit
    file_path, _ = QFileDialog.getOpenFileName(widget, "Chọn file DSSV", "", "CSV File (*.csv)")
    __setPathText(file_path, inputLine)

def save_path_broswe(widget: QWidget, inputLine: QLineEdit):
    # @params: widget: QWidget, inputLine: QLineEdit
    file_path, _ = QFileDialog.getSaveFileName(widget, "Chọn vị trí lưu", "", "PowerPoint File (*.pptx)")
    __setPathText(file_path, inputLine)