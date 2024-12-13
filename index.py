import sys
from classes.thread import CheckingThread
from src.ui.menu import Menu as MenuUI
from src.logging.error import exception
from globals import app

def __uncaught_exception_handler(exctype, value, traceback):
    exception(__name__, exctype, str(value), traceback)
    sys.exit(1)

sys.excepthook = __uncaught_exception_handler

if __name__ == "__main__":
    # Kiểm tra PowerPoint (thread song song)
    checking_thread = CheckingThread()
    checking_thread.start() 

    # Hiển thị menu
    menu = MenuUI()
    menu.show()

    # Chạy ứng dụng
    sys.exit(app.exec_()) 
