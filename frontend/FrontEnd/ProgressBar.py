from PySide6.QtWidgets import QWidget
from PySide6.QtUiTools import QUiLoader
from PySide6.QtCore import QFile
import sys
sys.path.append("FrontEnd/Resource")
import ProgressBarResource_rc 


def load_ui(ui_file: str) -> QWidget:
    """Load file .ui thành QWidget"""
    loader = QUiLoader()
    qfile = QFile(ui_file)
    if not qfile.open(QFile.ReadOnly):
        raise RuntimeError(f"Không mở được file UI: {ui_file}")

    ui = loader.load(qfile)
    qfile.close()

    if ui is None:
        raise RuntimeError(f"Không thể load UI: {ui_file}")
    return ui


def load_qss(qss_path: str) -> str:
    """Đọc nội dung file .qss (stylesheet)"""
    f = QFile(qss_path)
    if not f.open(QFile.ReadOnly | QFile.Text):
        print(f"Không mở được file QSS: {qss_path}")
        return ""
    return str(f.readAll(), encoding="utf-8")


class ProgressBarWidget(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        # load từ .ui của progress
        self.ui = load_ui("FrontEnd/UI/ProgressBar.ui")
        self.setStyleSheet(load_qss(":/rs/QSS/ProgressBar.qss"))

        # Đặt layout chính là widget đã load
        self.setLayout(self.ui.layout())
