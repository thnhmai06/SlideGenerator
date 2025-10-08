import sys
sys.path.append("Resource")
import ProgressBarResource_rc

from PySide6.QtWidgets import QWidget, QPlainTextEdit, QVBoxLayout
from PySide6.QtUiTools import QUiLoader
from PySide6.QtCore import QFile
from PySide6.QtGui import QIcon
from UILoader import UI



class LogPrintWidget(QWidget):
    def __init__(self, parent: QWidget = None, title: str = "Log"):
        super().__init__(parent)

        # Load UI
        self.uiLoader = UI()
        self.ui = self.uiLoader.load_ui("UI/LogPrint.ui")

        # Set layout and window properties
        self.setLayout(self.ui.layout())
        self.setFixedSize(600, 400)
        self.uiLoader.apply_stylesheet(self, [":/ProgressBar/QSS/LogPrintWindow.qss"])
        self.setWindowTitle(title)

        # Find log text edit
        self.text_edit: QPlainTextEdit = self.findChild(QPlainTextEdit, "log")
        if self.text_edit is None:
            raise RuntimeError("Không tìm thấy QPlainTextEdit có objectName='log'")


    #Cập nhật log
    def append_log(self, text: str):
        self.text_edit.appendPlainText(text)
