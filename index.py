import sys
from src.logger import error
from src.logger.debug import console_debug
from globals import app, pptx
from src.ui import menu


def __uncaught_exception_handler(exctype, value, traceback):
    error.exception(__name__, exctype, str(value), traceback)
    if pptx.instance:
        pptx.close_instance()
    if pptx.presentation:
        pptx.close_presentation()
    sys.exit(1)


sys.excepthook = __uncaught_exception_handler


def __is_Powerpoint_available():
    # Kiểm tra Powerpoint có sẵn không
    pptx.open_instance()
    if not pptx.instance:
        error.default(__name__, "no_powerpoint")
        sys.exit(1)
    console_debug(__name__, "had_powerpoint")


if __name__ == "__main__":
    __is_Powerpoint_available()
    menu_window = menu.Menu()
    menu_window.show()  # Hiển thị menu
    sys.exit(app.exec_())  # Chạy ứng dụng
