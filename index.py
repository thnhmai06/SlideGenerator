import sys
import traceback
from PyQt5 import QtWidgets
from ui.resources import qInitResources
from ui import menu, progress
from logger import error
from logger.debug import default as console_debug

def __uncaught_exception_handler(exctype, value, tb):
    error.exception(__name__, exctype, str(value), tb)
    sys.exit(1)
sys.excepthook = __uncaught_exception_handler


app = QtWidgets.QApplication(sys.argv)
menu_window = menu.Ui()
progress_window = progress.Ui()

if __name__ == "__main__":
    # Nạp resources
    console_debug(__name__, "load_resources")
    qInitResources()

    # Hiển thị menu
    menu_window.show()

    # Chạy ứng dụng
    sys.exit(app.exec_())
        