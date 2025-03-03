import sys
from typing import Type, Any
from classes.threads import PowerPointCheckThread
from src.ui.menu import Menu as MenuUI
from src.logging.critical import critical
from src.logging.debug import console_debug
from globals import app, OPEN_TIME
from translations import format_text, get_current_language, register_language_change_callback

def language_change_handler(new_lang: str) -> None:
    """
    Xử lý sự kiện khi ngôn ngữ thay đổi.
    
    Args:
        new_lang (str): Mã ngôn ngữ mới.
    """
    console_debug(__name__, "language_changed", new_lang)

def exception_handler(exc_type: Type[BaseException], exc_value: BaseException, exc_traceback: Any) -> None:
    """
    Xử lý các ngoại lệ không bắt được trong ứng dụng.
    
    Args:
        exc_type: Loại ngoại lệ.
        exc_value: Giá trị ngoại lệ.
        exc_traceback: Thông tin traceback.
    """
    error_message = format_text("errors.uncaught_exception", error=str(exc_value))
    critical(__name__, exc_value, exc_traceback, error_message)

def setup_application() -> None:
    """
    Thiết lập các cấu hình ban đầu cho ứng dụng.
    """
    # Đặt hook để xử lý các ngoại lệ không bắt được
    sys.excepthook = exception_handler
    
    # Đăng ký callback khi ngôn ngữ thay đổi
    register_language_change_callback(language_change_handler)
    
    # Hiển thị ngày giờ lúc mở và ngôn ngữ hiện tại
    console_debug(__name__, "open_time", OPEN_TIME.strftime("%Y-%m-%d %H:%M:%S"))
    console_debug(__name__, "current_language", get_current_language())

def main() -> int:
    """
    Hàm chính chạy ứng dụng.
    
    Returns:
        int: Mã trạng thái khi thoát.
    """
    # Thiết lập ứng dụng
    setup_application()
    
    try:
        # Kiểm tra PowerPoint (thread song song)
        checking_thread = PowerPointCheckThread()
        checking_thread.start()

        # Hiển thị menu
        menu = MenuUI()
        menu.show()

        # Chạy ứng dụng và trả về mã thoát
        return app.exec_()
    except Exception as e:
        error_message = format_text("errors.application.general", error=str(e))
        # Lấy traceback hiện tại
        tb = sys.exc_info()[2]
        critical(__name__, e, tb, error_message)
        return 1

if __name__ == "__main__":
    sys.exit(main())
