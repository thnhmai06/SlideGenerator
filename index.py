import sys
from classes.threads import CheckingThread
from src.ui.menu import Menu as MenuUI
from src.logging.critical import critical
from src.logging.debug import console_debug
from globals import app, OPEN_TIME

# Đặt hook để xử lý các ngoại lệ không bắt được
sys.excepthook = lambda type, exception, traceback: {
    critical(__name__, exception, traceback),
    sys.exit(1)
}

if __name__ == "__main__":
    # Hiển thị ngày giờ lúc mở
    console_debug(__name__, "open_time", OPEN_TIME.strftime("%Y-%m-%d %H:%M:%S"))

    # Kiểm tra PowerPoint (thread song song)
    checking_thread = CheckingThread()
    checking_thread.start()

    # Hiển thị menu
    menu = MenuUI()
    menu.show()

    # Chạy ứng dụng
    sys.exit(app.exec_())
