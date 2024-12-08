import sys
from PyQt5.QtWidgets import QApplication
from classes.thread import CheckingThread
from src.ui.menu import Menu as MenuUI
from src.logging.error import exception

def __uncaught_exception_handler(exctype, value, traceback):
    exception(__name__, exctype, str(value), traceback)
    sys.exit(1)

sys.excepthook = __uncaught_exception_handler

if __name__ == "__main__":
    app = QApplication(sys.argv)

    checking_thread = CheckingThread()
    checking_thread.start()  # Kiểm tra PowerPoint

    menu = MenuUI()
    menu.show()  # Hiển thị menu
    sys.exit(app.exec_())  # Chạy ứng dụng
