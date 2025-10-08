from PySide6.QtWidgets import QWidget, QMainWindow
from PySide6.QtUiTools import QUiLoader
from PySide6.QtCore import QFile


# Dùng để load UI, set style cho UI 
class UI:
    def __init__(self):
        pass



    def load_ui(self, ui_file: str) -> QMainWindow:
        loader = QUiLoader()
        ui_file_obj = QFile(ui_file)
        if not ui_file_obj.open(QFile.ReadOnly):
            raise RuntimeError(f"Không thể mở file UI: {ui_file}")

        ui = loader.load(ui_file_obj)
        ui_file_obj.close()

        if ui is None:
            raise RuntimeError(f"Không thể load UI: {ui_file}")
        return ui



    def load_qss(self, qss_path: str) -> str:
        f = QFile(qss_path)
        if not f.open(QFile.ReadOnly | QFile.Text):
            print(f"Không mở được file QSS: {qss_path}")
            return ""
        return str(f.readAll(), encoding="utf-8")



    def apply_stylesheet(self, window: QWidget, qss_files: list[str]) -> None:
        combined_qss = ""
        for path in qss_files:
            combined_qss += self.load_qss(path) + "\n"
        if combined_qss:
            window.setStyleSheet(combined_qss)
