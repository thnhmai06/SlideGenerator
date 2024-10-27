from PyQt5 import QtCore, QtGui, QtWidgets
import sys

try:
    from gui import error, information, menu, progress
except:
    import create_gui_script
    create_gui_script.UIconverter() # Chuyển .ui -> .py
    create_gui_script.QRCconverter() # Chuyển .qrc -> .py

    try:
        from gui import error, information, menu, progress
    except:
        print("ERR: Khong the chuyen file UI thanh code")
        sys.exit(1)
app = QtWidgets.QApplication(sys.argv)
window = QtWidgets.QMainWindow()

#? Quy định trước HTML
def successText(success, fails):
    return f'<html><head/><body><p><span style=" font-size:10pt; font-weight:600;">Tạo Slide hoàn tất!</span></p><p><span style=" font-size:10pt;">Thành công: {success}/{success+fails}| Thất bại: {fails}/{success+fails}</span></p></body></html>'
noPowerPointText = '<html><head/><body><p><span style=" font-size:10pt; font-weight:600;">Vui lòng cài đặt Microsoft PowerPoint (Office) <br/>để sử dụng phần mềm này!</span><br/></p></body></html>'

#? Các hàm hiển thị UI
def showMenu():
    menu.Ui_menu().setupUi(window)
    window.show()
def showError(context):
    #Tạo mới app, window khác
    popup_app = QtWidgets.QApplication(sys.argv)
    popup_window = QtWidgets.QMainWindow()
    
    # Thiết đặt: UI
    ui = error.Ui_error_notice()
    ui.setupUi(popup_window) # Xây dựng UI mẫu
    ui.okbutton.clicked.connect(popup_app.quit) # Nút OK sẽ đóng cửa sổ

    #Thiết đặt: window
    popup_window.show() # Xây dựng (Hiển thị) window

    #In ra lỗi
    print(f"ERROR: {context}")

    #Chạy hộp thoại
    sys.exit(popup_app.exec_())
def showInfo(context):
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

if __name__ == "__main__":
    showMenu()

    #Chạy ứng dụng
    sys.exit(app.exec_())