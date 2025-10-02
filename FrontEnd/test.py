import sys
from pathlib import Path
from PySide6.QtWidgets import (
    QApplication, QMainWindow, QListWidget,
    QStackedWidget, QPushButton, QWidget, QHBoxLayout
)

from PySide6.QtCore import QUrl
from PySide6.QtGui import QDesktopServices
from PySide6.QtUiTools import QUiLoader
from PySide6.QtCore import QFile
from PySide6.QtWidgets import QFileDialog, QLineEdit, QPushButton
from ProgressBar import ProgressBarWidget   
sys.path.append("Resource")
import MainResource_rc


def load_ui(ui_file: str) -> QMainWindow:
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
    f = QFile(qss_path)
    if not f.open(QFile.ReadOnly | QFile.Text):
        print(f"Không mở được file QSS: {qss_path}")
        return ""
    return str(f.readAll(), encoding="utf-8")


def apply_stylesheet(window: QMainWindow, qss_files: list[str]) -> None:
    combined_qss = ""
    for path in qss_files:
        combined_qss += load_qss(path) + "\n"
    if combined_qss:
        window.setStyleSheet(combined_qss)


def setup_navigation(window: QMainWindow) -> None:
    listWidget: QListWidget = window.findChild(QListWidget, "listWidget")
    stackedWidget: QStackedWidget = window.findChild(QStackedWidget, "stackedWidget")

    # Ánh xạ ListWidget index → StackedWidget index
    # Chú ý trong qt designer phải chọn trang khác trang đầu rồi mới save vì nếu không khi mở app lên sidebar lỗi không hightlight ô đầu
    mapping = {0: 0, 1: 2, 2: 3}
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

    # Nút About
    about_btn: QPushButton = window.findChild(QPushButton, "about")
    if about_btn:
        about_btn.clicked.connect(lambda: (stackedWidget.setCurrentIndex(4), listWidget.clearSelection()))

    # Nút Start
    start_btn: QPushButton = window.findChild(QPushButton, "start")
    if start_btn:
        start_btn.clicked.connect(lambda: stackedWidget.setCurrentIndex(3))


def add_progress_bar(window: QMainWindow):
    container = window.findChild(QWidget, "progressContainer")
    if not container:
        print("Không tìm thấy progressContainer")
        return

    if container.layout() is None:
        container.setLayout(QHBoxLayout())

    progress = ProgressBarWidget()
    container.layout().insertWidget(0, progress)





def setup_browse_buttons(window):
    browse_csv_file_btn: QPushButton = window.findChild(QPushButton, "csvFileBtn")
    browse_pptx_file_btn: QPushButton = window.findChild(QPushButton, "pptxFileBtn")
    browse_save_folder_btn: QPushButton = window.findChild(QPushButton, "saveFolderBtn")
    csv_file_edit: QLineEdit = window.findChild(QLineEdit, "csvFilePathEdit")
    pptx_file_edit: QLineEdit = window.findChild(QLineEdit, "pptxFilePathEdit")
    save_folder_edit: QLineEdit = window.findChild(QLineEdit, "saveFolderPathEdit")

    def browse_csv_file():
        start_dir = str(Path.home() / "Documents")
        file_path, _ = QFileDialog.getOpenFileName(
            window, "Chọn file", start_dir, "Excel Files (*.csv);;All Files (*)"
        )
        if file_path and csv_file_edit:
            csv_file_edit.setText(file_path)

    def browse_pptx_file():
        start_dir = str(Path.home() / "Documents")
        file_path, _ = QFileDialog.getOpenFileName(
            window, "Chọn file", start_dir, "PowerPoint Files (*.pptx);;All Files (*)"
        )
        if file_path and pptx_file_edit:
            pptx_file_edit.setText(file_path)

    def browse_save_folder():
        start_dir = str(Path.home() / "Documents")
        folder_path = QFileDialog.getExistingDirectory(window, "Chọn folder", start_dir)
        if folder_path and save_folder_edit:
            save_folder_edit.setText(folder_path)

    if browse_csv_file_btn:
        browse_csv_file_btn.clicked.connect(browse_csv_file)
    if browse_pptx_file_btn:
        browse_pptx_file_btn.clicked.connect(browse_pptx_file)
    if browse_save_folder_btn:
        browse_save_folder_btn.clicked.connect(browse_save_folder)



def setup_readme_link(window):
    readme_btn = window.findChild(QPushButton, "readmeLink")
    if not readme_btn:
        print("Không tìm thấy nút readmeLink")
        return

    script_dir = Path(__file__).parent
    file_path = (script_dir.parent / "README.md").resolve()  

    def open_readme():
        if file_path.exists():
            QDesktopServices.openUrl(QUrl.fromLocalFile(str(file_path)))
        else:
            print("File text.txt không tồn tại:", file_path)

    readme_btn.clicked.connect(open_readme)



def setup_github_link(window):
    readme_btn = window.findChild(QPushButton, "githubLink")
    if not readme_btn:
        print("Không tìm thấy nút githubLink")
        return

    url = "https://github.com/thnhmai06/tao-slide-tot-nghiep"  # <-- project github

    def open_url():
        QDesktopServices.openUrl(QUrl(url))

    readme_btn.clicked.connect(open_url)




def main():
    app = QApplication(sys.argv)

    window = load_ui("UI/Main.ui")

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
    setup_navigation(window)

    #"""
    # test: thêm progress mỗi lần chạy
    for i in range(3):
        add_progress_bar(window)
    #"""

    setup_browse_buttons(window)
    setup_readme_link(window)
    setup_github_link(window)

    window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()