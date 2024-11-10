import sys
import traceback
from PyQt5 import QtWidgets
from ui.resources import qInitResources, qCleanupResources
from ui import menu, progress
from logger import error

app = QtWidgets.QApplication(sys.argv)
menu_window = menu.Ui()
progress_window = progress.Ui()

if __name__ == "__main__":
    try:
        # Nạp resources
        qInitResources()

        # Hiển thị menu
        menu_window.show()

        # Chạy ứng dụng
        sys.exit(app.exec_())
    
    except Exception as err:
        error.expection(err, traceback.format_exc())

    finally:
        # Cleanup resources
        qCleanupResources()