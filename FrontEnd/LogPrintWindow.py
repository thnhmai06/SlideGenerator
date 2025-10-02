from PySide6.QtWidgets import QApplication, QDialog, QFileDialog
from PySide6.QtUiTools import QUiLoader
from PySide6.QtCore import QFile
import sys

class LogDialog(QDialog):
    def __init__(self):
        super().__init__()
        loader = QUiLoader()
        ui_file = QFile("FrontEnd/UI/LogPrint.ui")   # file .ui bạn thiết kế dạng Dialog
        ui_file.open(QFile.ReadOnly)
        self.dialog = loader.load(ui_file, self)
        ui_file.close()

        # Lấy widget
        self.log_edit = self.dialog.findChild(type(self.dialog.logTextEdit), "logTextEdit")
        self.clear_btn = self.dialog.findChild(type(self.dialog.clearBtn), "clearBtn")
        self.save_btn = self.dialog.findChild(type(self.dialog.saveBtn), "saveBtn")

        # Gắn event
        if self.clear_btn:
            self.clear_btn.clicked.connect(self.clear_log)
        if self.save_btn:
            self.save_btn.clicked.connect(self.save_log)

    def append_log(self, text: str):
        """Thêm log realtime"""
        self.log_edit.appendPlainText(text)

    def clear_log(self):
        self.log_edit.clear()

    def save_log(self):
        file_path, _ = QFileDialog.getSaveFileName(self, "Save log", "", "Text Files (*.txt)")
        if file_path:
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(self.log_edit.toPlainText())

if __name__ == "__main__":
    app = QApplication(sys.argv)
    dlg = LogDialog()
    dlg.dialog.show()   # vì mình load .ui vào self.dialog
    sys.exit(app.exec())
