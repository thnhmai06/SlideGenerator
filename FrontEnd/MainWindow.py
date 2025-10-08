import sys
sys.path.append("Resource")
import MainResource_rc

from PySide6.QtWidgets import (
    QApplication, QMainWindow, QListWidget, QStackedWidget,
    QPushButton, QWidget, QHBoxLayout, QFileDialog, QLineEdit
)
from PySide6.QtGui import QDesktopServices
from ProgressBar import ProgressBarWidget
from LogPrintWindow import LogPrintWidget
from PySide6.QtCore import QUrl
from pathlib import Path
from UILoader import UI







class MainController:
    def __init__(self, ui_file: str):
        self.uiLoader = UI()
        self.window: QMainWindow = self.uiLoader.load_ui(ui_file)

        # Store widgets references
        self.listWidget: QListWidget = self.window.findChild(QListWidget, "listWidget")
        self.stackedWidget: QStackedWidget = self.window.findChild(QStackedWidget, "stackedWidget")

        # Browse widgets
        self.csv_file_edit: QLineEdit = self.window.findChild(QLineEdit, "csvFilePathEdit")
        self.pptx_file_edit: QLineEdit = self.window.findChild(QLineEdit, "pptxFilePathEdit")
        self.save_folder_edit: QLineEdit = self.window.findChild(QLineEdit, "saveFolderPathEdit")

        self.browse_csv_file_btn: QPushButton = self.window.findChild(QPushButton, "csvFileBtn")
        self.browse_pptx_file_btn: QPushButton = self.window.findChild(QPushButton, "pptxFileBtn")
        self.browse_save_folder_btn: QPushButton = self.window.findChild(QPushButton, "saveFolderBtn")

        # Other buttons
        self.about_btn: QPushButton = self.window.findChild(QPushButton, "about")
        self.start_btn: QPushButton = self.window.findChild(QPushButton, "start")
        self.readme_btn: QPushButton = self.window.findChild(QPushButton, "readmeLink")
        self.github_btn: QPushButton = self.window.findChild(QPushButton, "githubLink")

        # Progress bars
        self.progress_bars: list[ProgressBarWidget] = []

        # Setup
        self.apply_stylesheets()
        self.setup_navigation()
        self.setup_browse_buttons()
        self.setup_links()



    def apply_stylesheets(self):
        qss_files = [
            ":/QSS/QSS/Sidebar.qss",
            ":/QSS/QSS/MainWindow.qss",
            ":/QSS/QSS/InputMenu.qss",
            ":/QSS/QSS/SettingMenu.qss",
            ":/QSS/QSS/DownloadMenu.qss",
            ":/QSS/QSS/ProcessMenu.qss",
            ":/QSS/QSS/AboutMenu.qss",
        ]
        self.uiLoader.apply_stylesheet(self.window, qss_files)



    # Lấy đường dẫn của input 
    def get_file_paths(self) -> dict:
        """
        Lấy các đường dẫn input/output từ các ô duyệt.
        Trả về dict với key: csv, pptx, save_folder
        """
        return {
            "csv": self.csv_file_edit.text() if self.csv_file_edit else "",
            "pptx": self.pptx_file_edit.text() if self.pptx_file_edit else "",
            "save_folder": self.save_folder_edit.text() if self.save_folder_edit else "",
        }



    # Dùng để map các tab với window tương ứng 
    # Chú ý trong qt designer phải chọn trang khác trang đầu rồi mới save vì nếu không khi mở app lên sidebar lỗi không hightlight ô đầu
    def setup_navigation(self):
        mapping = {0: 0, 1: 2, 2: 3}
        reverse_mapping = {v: k for k, v in mapping.items()}

        def on_item_changed(row: int):
            if row in mapping:
                self.stackedWidget.setCurrentIndex(mapping[row])

        def on_page_changed(index: int):
            if index in reverse_mapping:
                self.listWidget.setCurrentRow(reverse_mapping[index])
            else:
                self.listWidget.clearSelection()
                self.listWidget.setCurrentRow(-1)

        self.listWidget.currentRowChanged.connect(on_item_changed)
        self.stackedWidget.currentChanged.connect(on_page_changed)

        if self.about_btn:
            self.about_btn.clicked.connect(
                lambda: (self.stackedWidget.setCurrentIndex(4), self.listWidget.clearSelection())
            )

        if self.start_btn:
            self.start_btn.clicked.connect(lambda: self.stackedWidget.setCurrentIndex(3))



    # Thêm thanh tiến trình 
    def add_progress_bar(self) -> int:
        container = self.window.findChild(QWidget, "progressContainer")
        if not container:
            print("Không tìm thấy progressContainer")
            return -1

        if container.layout() is None:
            container.setLayout(QHBoxLayout())

        progress = ProgressBarWidget()
        container.layout().insertWidget(0, progress)

        self.progress_bars.append(progress)
        return len(self.progress_bars) - 1



    #Cập nhật phần trăm của thanh tiến trình
    def update_progress(self, bar_id: int, value: int, label: str):
        if 0 <= bar_id < len(self.progress_bars):
            self.progress_bars[bar_id].setValue(value)
            self.progress_bars[bar_id].setLabel(label)
        else:
            print(f"ProgressBar ID {bar_id} không tồn tại")



    #Cập nhật log của thanh tiến trình 
    def update_log(self, bar_id: int, text: str):
        if 0 <= bar_id < len(self.progress_bars):
            self.progress_bars[bar_id].append_log(text)



    #Đường dẫn input và output
    def setup_browse_buttons(self):
        def browse_file(edit_widget: QLineEdit, file_filter: str):
            start_dir = str(Path.home() / "Documents")
            file_path, _ = QFileDialog.getOpenFileName(self.window, "Chọn file", start_dir, file_filter)
            if file_path and edit_widget:
                edit_widget.setText(file_path)

        def browse_folder(edit_widget: QLineEdit):
            start_dir = str(Path.home() / "Documents")
            folder_path = QFileDialog.getExistingDirectory(self.window, "Chọn folder", start_dir)
            if folder_path and edit_widget:
                edit_widget.setText(folder_path)

        if self.browse_csv_file_btn:
            self.browse_csv_file_btn.clicked.connect(lambda: browse_file(self.csv_file_edit, "Excel Files (*.csv);;All Files (*)"))
        if self.browse_pptx_file_btn:
            self.browse_pptx_file_btn.clicked.connect(lambda: browse_file(self.pptx_file_edit, "PowerPoint Files (*.pptx);;All Files (*)"))
        if self.browse_save_folder_btn:
            self.browse_save_folder_btn.clicked.connect(lambda: browse_folder(self.save_folder_edit))


    #Đường dẫn readme và github 
    def setup_links(self):
        # Readme
        if self.readme_btn:
            script_dir = Path(__file__).parent
            file_path = (script_dir.parent / "README.md").resolve()

            def open_readme():
                if file_path.exists():
                    QDesktopServices.openUrl(QUrl.fromLocalFile(str(file_path)))
                else:
                    print("File README.md không tồn tại:", file_path)

            self.readme_btn.clicked.connect(open_readme)

        # GitHub
        if self.github_btn:
            url = "https://github.com/thnhmai06/tao-slide-tot-nghiep"  #<--- link project

            self.github_btn.clicked.connect(lambda: QDesktopServices.openUrl(QUrl(url)))



def main():
    app = QApplication(sys.argv)
    controller = MainController("UI/Main.ui")
    controller.window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
