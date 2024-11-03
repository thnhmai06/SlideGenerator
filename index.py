import sys, contextlib
from PyQt5 import QtCore, QtGui, QtWidgets
from src.script import showWidget
from src.ui.resources import qInitResources

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
        print(err)
        showWidget.Error(str(err))
        