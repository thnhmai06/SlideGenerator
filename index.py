import sys
from PyQt5 import QtCore, QtGui, QtWidgets
from src.script import showUI
from src.ui.resources import qInitResources

app = QtWidgets.QApplication(sys.argv)
window = QtWidgets.QMainWindow()

if __name__ == "__main__":
    # Nạp resources
    qInitResources()
    # Hiển thị menu
    showUI.Menu(window)
    
    #Chạy ứng dụng
    sys.exit(app.exec_())