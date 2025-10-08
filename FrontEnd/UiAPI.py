from PySide6.QtWidgets import QApplication
from MainWindow import MainController


# API giao tiếp với giao diện 

class UiAPI:
    def __init__(self, app: QApplication = None):
        self.app = app
        self.controller = None

    def init_controller(self):
        self.controller = MainController("UI/Main.ui")
        self.controller.window.show()


    # Thêm thanh tiến trình 
    def addProgress(self) -> int:
        return self.controller.add_progress_bar()


   	# Cập nhật thanh tiến trình (id, giá trị %, tên thẻ)
    def updateProgress(self, bar_id: int, value: int, label: str):
        self.controller.update_progress(bar_id, value, label)


    # Cập nhật log của thanh tiến trình với id tương ứng 
    def updateLog(self, bar_id: int, text: str):
        self.controller.update_log(bar_id, text)


    # Lấy đường dẫn input của các file csv, pptx, save folder 
    def getPaths(self) -> dict:
        return self.controller.get_file_paths()






