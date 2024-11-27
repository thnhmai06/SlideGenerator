import sys
from src.logger import error
from src.logger.debug import console_debug
from globals import app, pptx_instance
from src.ui import menu

def __uncaught_exception_handler(exctype, value, tb):
    error.exception(__name__, exctype, str(value), tb)
    if pptx_instance:
        pptx_instance.quit()
    sys.exit(1)
sys.excepthook = __uncaught_exception_handler


def __is_Powerpoint_available():
    # Kiểm tra Powerpoint có sẵn không
    if not pptx_instance:
        error.default(__name__, "no_powerpoint")
        sys.exit(0)
    console_debug(__name__, "had_powerpoint")

if __name__ == "__main__":
    __is_Powerpoint_available()
    menu_window = menu.Menu()
    menu_window.show()  # Hiển thị menu
    sys.exit(app.exec_())  # Chạy ứng dụng
