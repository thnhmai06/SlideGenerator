import sys
from pathlib import Path
from PySide6.QtWidgets import QApplication, QMainWindow, QListWidget, QStackedWidget, QPushButton
from PySide6.QtUiTools import QUiLoader
from PySide6.QtCore import QFile
sys.path.append("Resource")
import Resource_rc  # Resource_rc chứa icon, qss, hình ảnh đã biên dịch vào file .qrc


def load_ui(ui_file: str) -> QMainWindow:
    """Load file .ui (tạo từ Qt Designer) thành cửa sổ PySide6"""
    loader = QUiLoader()
    ui_file_obj = QFile(ui_file)
    if not ui_file_obj.open(QFile.ReadOnly):
        raise RuntimeError(f"Không thể mở file UI: {ui_file}")

    ui = loader.load(ui_file_obj)
    ui_file_obj.close()

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


def apply_stylesheet(window: QMainWindow, qss_files: list[str]) -> None:
    """Gộp nhiều file QSS lại và áp dụng cho toàn bộ cửa sổ"""
    combined_qss = ""
    for path in qss_files:
        combined_qss += load_qss(path) + "\n"
    if combined_qss:
        window.setStyleSheet(combined_qss)


def setup_navigation(window: QMainWindow) -> None:
    """Thiết lập điều hướng giữa ListWidget và StackedWidget"""
    listWidget: QListWidget = window.findChild(QListWidget, "listWidget")
    stackedWidget: QStackedWidget = window.findChild(QStackedWidget, "stackedWidget")

    # Ánh xạ ListWidget index → StackedWidget index
    mapping = {0: 0, 1: 2, 2: 1, 3: 3}
    reverse_mapping = {v: k for k, v in mapping.items()}

    def on_item_changed(row: int):
        if row in mapping:
            stackedWidget.setCurrentIndex(mapping[row])

    def on_page_changed(index: int):
        if index in reverse_mapping:
            listWidget.setCurrentRow(reverse_mapping[index])
        else:
            listWidget.clearSelection()
            listWidget.setCurrentRow(-1)  # không chọn gì

    listWidget.currentRowChanged.connect(on_item_changed)
    stackedWidget.currentChanged.connect(on_page_changed)

    # Nút About → nhảy sang trang 4
    about_btn: QPushButton = window.findChild(QPushButton, "about")
    if about_btn:
        about_btn.clicked.connect(lambda: (stackedWidget.setCurrentIndex(4), listWidget.clearSelection()))

    # Nút Start → nhảy sang trang 3
    start_btn: QPushButton = window.findChild(QPushButton, "start")
    if start_btn:
        start_btn.clicked.connect(lambda: stackedWidget.setCurrentIndex(3))


def main():
    app = QApplication(sys.argv)

    # Load giao diện chính
    window = load_ui("UI/Main.ui")

    # Load style QSS
    qss_files = [
        ":/QSS/QSS/Sidebar.qss",
        ":/QSS/QSS/MainWindow.qss",
        ":/QSS/QSS/InputMenu.qss",
        ":/QSS/QSS/SettingMenu.qss",
        ":/QSS/QSS/DownloadMenu.qss",
        ":/QSS/QSS/ProcessMenu.qss",
        ":/QSS/QSS/AboutMenu.qss",
    ]
    apply_stylesheet(window, qss_files)

    # Setup navigation logic
    setup_navigation(window)

    # Hiển thị cửa sổ
    window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
