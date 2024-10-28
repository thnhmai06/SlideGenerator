import sys
from PyQt5 import QtCore, QtGui, QtWidgets
from src import *

try: #Thử Import module chứa UI
    from src.ui import menu
except: # UI chưa được chuyển thành code
    convertUI.UIconverter() # Chuyển .ui -> .py
    convertUI.QRCconverter() # Chuyển .qrc -> .py

    try: # Thử lại
        from src.ui import menu
    except Exception as err: # Báo lỗi
        print("ERR: Khong the chuyen file UI thanh code")
        print(err)
        print('\n')
        input("Press Enter to continue...")
        sys.exit(1)
app = QtWidgets.QApplication(sys.argv)
window = QtWidgets.QMainWindow()

if __name__ == "__main__":
    # Hiển thị menu
    showUI.Menu(window)

    #Chạy ứng dụng
    sys.exit(app.exec_())