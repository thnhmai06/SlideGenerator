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

if __name__ == "__main__":
    # Kiểm tra Powerpoint có sẵn không
    if not pptx_instance:
        error.default(__name__, "no_powerpoint")
        sys.exit(0)
    console_debug(__name__, "had_powerpoint")

    # Hiển thị menu
    menu_window.show()

    # Chạy ứng dụng
    sys.exit(app.exec_())
        