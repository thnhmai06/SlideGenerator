import sys
from PyQt5 import QtCore, QtGui, QtWidgets
from PyQt5.QtWidgets import QHeaderView
from src.ui import error, information, progress, menu

#? Các hàm hiển thị UI
def Menu(window):
    ui = menu.Ui_menu()
    ui.setupUi(window) # Xây dựng UI mẫu

    window.show()
def Error(context):
    #Tạo mới app, window khác
    popup_app = QtWidgets.QApplication(sys.argv)
    popup_window = QtWidgets.QMainWindow()
    
    # Thiết đặt: UI
    ui = error.Ui_error()
    ui.setupUi(popup_window) # Xây dựng UI mẫu
    ui.okbutton.clicked.connect(popup_app.quit) # Nút OK sẽ đóng cửa sổ

    #In ra lỗi ở phần Chi tiết
    ui.details.setPlainText(context)
    ui.details.setReadOnly(True)

    #Thiết đặt: window
    popup_window.show() # Xây dựng (Hiển thị) window

    #Chạy hộp thoại
    sys.exit(popup_app.exec_())
def Info(context):
    #Tạo mới app, window khác
    popup_app = QtWidgets.QApplication(sys.argv)
    popup_window = QtWidgets.QMainWindow()
    
    # Thiết đặt: UI
    ui = information.Ui_information_notice()
    ui.setupUi(popup_window) # Xây dựng UI mẫu
    ui.label.setText(context) # Đặt nội dung của thông báo
    ui.okbutton.clicked.connect(popup_app.quit) # Nút OK sẽ đóng cửa sổ
    
    #Thiết đặt: window
    popup_window.show() # Xây dựng (Hiển thị) window

    #Chạy hộp thoại
    sys.exit(popup_app.exec_())
