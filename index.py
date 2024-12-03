import sys
from PyQt5.QtWidgets import QApplication
from classes.thread import CheckingThread
from src.logging.error import exception
from src.ui import menu

def __uncaught_exception_handler(exctype, value, traceback):
    exception(__name__, exctype, str(value), traceback)
    sys.exit(1)

sys.excepthook = __uncaught_exception_handler

if __name__ == "__main__":
    app = QApplication(sys.argv)
    powerpoint_checker_thread = CheckingThread()
    powerpoint_checker_thread.start()

    menu_window = menu.Menu()
    menu_window.show()  # Hiển thị menu
    sys.exit(app.exec_())  # Chạy ứng dụng
