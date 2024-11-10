import sys
import traceback
from PyQt5 import QtWidgets
from ui.resources import qInitResources
from ui import menu, progress
from logger import error
from logger.debug import default as console_debug

app = QtWidgets.QApplication(sys.argv)
menu_window = menu.Ui()
progress_window = progress.Ui()

if __name__ == "__main__":
    try:
        # Nạp resources
        console_debug(__name__, "load_resources")
        qInitResources()

        # Hiển thị menu
        menu_window.show()

        # Chạy ứng dụng
        app.exec_()
    except Exception as err:
        error.expection(__name__, err, traceback.format_exc())