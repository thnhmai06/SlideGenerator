import sys
sys.path.append("Resource")
import ProgressBarResource_rc 

from PySide6.QtWidgets import QWidget, QProgressBar, QLabel, QPushButton
from LogPrintWindow import LogPrintWidget
from PySide6.QtUiTools import QUiLoader
from PySide6.QtCore import QFile
from UILoader import UI



class ProgressBarWidget(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.uiLoader = UI()
        self.ui = self.uiLoader.load_ui("UI/ProgressBar.ui")
        self.uiLoader.apply_stylesheet(self, [":/ProgressBar/QSS/ProgressBar.qss"])

        self.progress_bar: QProgressBar = self.ui.findChild(QProgressBar, "progressBar")
        self.progress_label: QLabel = self.ui.findChild(QLabel, "label")
        self.log_button: QPushButton = self.ui.findChild(QPushButton, "logButton")

        if not self.progress_bar:
            raise RuntimeError("Thiếu progressBar trong ProgressBar.ui")
        if not self.progress_label:
            raise RuntimeError("Thiếu label trong ProgressBar.ui")
        if not self.log_button:
            raise RuntimeError("Thiếu logButton trong ProgressBar.ui")

        self.setLayout(self.ui.layout())

        # mỗi progressbar có log riêng
        self.log_window = LogPrintWidget()
        self.log_button.clicked.connect(self.show_log)



        


    def update_log_title(self):
        title = self.progress_label.text() if self.progress_label else "Log"
        self.log_window = LogPrintWidget(title=title)

    def setValue(self, value: int):
        self.progress_bar.setValue(value)

    def value(self) -> int:
        return self.progress_bar.value()

    def setLabel(self, label: str):
        self.progress_label.setText(label)
        self.update_log_title()

    def show_log(self):
        self.log_window.show()
        self.log_window.raise_()
        self.log_window.activateWindow()

    def append_log(self, text: str):
        self.log_window.append_log(text)