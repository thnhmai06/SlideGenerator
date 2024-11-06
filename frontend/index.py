import sys, contextlib
from PyQt5 import QtCore, QtGui, QtWidgets
from src.script import showWidget
from src.ui.resources import qInitResources
from src.ui import diaglogs
import traceback

app = QtWidgets.QApplication(sys.argv)

if __name__ == "__main__":
    try:
        # Nạp resources
        qInitResources()
        # Hiển thị menu
        showWidget.Menu()

        #Chạy ứng dụng
        sys.exit(app.exec_())
    except Exception as err:
        print(traceback.format_exc())
        diaglogs.show_error("Đã xảy ra lỗi không xác định", str(err), traceback.format_exc())
        