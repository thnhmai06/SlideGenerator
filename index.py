import sys
from PyQt5 import QtWidgets
from ui import menu, progress
from logger import error
from logger.debug import console_debug
from globals import pptx_instance

def __uncaught_exception_handler(exctype, value, tb):
    error.exception(__name__, exctype, str(value), tb)
    sys.exit(1)
sys.excepthook = __uncaught_exception_handler


app = QtWidgets.QApplication(sys.argv)
menu_window = menu.Ui()
progress_window = progress.Ui()

def __is_Powerpoint_available():
    # Kiểm tra Powerpoint có sẵn không
    if not pptx_instance:
        error.default(__name__, "no_powerpoint")
        sys.exit(0)
    console_debug(__name__, "had_powerpoint")

if __name__ == "__main__":
    __is_Powerpoint_available()
    menu_window.show() # Hiển thị menu
    sys.exit(app.exec_()) # Chạy ứng dụng
        