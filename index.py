import sys
from PyQt5 import QtCore, QtGui, QtWidgets
from src import *

app = QtWidgets.QApplication(sys.argv)
window = QtWidgets.QMainWindow()

if __name__ == "__main__":
    # Hiển thị menu
    showUI.Menu(window)
    
    #Chạy ứng dụng
    sys.exit(app.exec_())