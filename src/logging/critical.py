import traceback
from typing import Any
from src.ui.diaglogs import error as show_error_diaglog
from translations import get_text
from src.logging import critical as log_critical

def console_critical(where: str, content: str) -> None:
    """
    Ghi log lỗi nghiêm trọng ra console.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        content (str): Nội dung lỗi nghiêm trọng.
    """
    log_critical(where, content)

def critical(
    where: str,
    expection: Exception,
    tb: Any,
    message: str = None,
    window_name: str = None,
) -> None:
    """
    Ghi log lỗi nghiêm trọng (Expection xảy ra) và hiển thị hộp thoại lỗi.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        expection (Exception): Giá trị Expection.
        tb (Any): Traceback của Expection.
        message (str, optional): Thông báo tùy chỉnh. Mặc định là None.
        window_name (str, optional): Tên cửa sổ. Mặc định lấy từ file dịch.
    """
    expection_type = expection.__class__.__name__
    
    # Lấy tiêu đề từ file dịch hoặc sử dụng thông báo tùy chỉnh
    if message:
        title = message
    else:
        title = f"{get_text('diaglogs.error.exception')}\n\n{expection_type}: {expection}"
    
    # Xử lý traceback
    try:
        if tb is None:
            details = ""
        elif isinstance(tb, traceback.StackSummary):
            details = "".join(tb.format())
        elif hasattr(tb, 'tb_frame'):  # Đây là traceback thực sự
            details = "".join(traceback.format_tb(tb))
        else:
            # Thử chuyển đổi thành chuỗi
            details = str(tb)
    except Exception as e:
        details = f"Không thể xử lý traceback: {str(e)}"
    
    # Ghi log vào console
    error_message = f"{title}\n{details}"
    console_critical(where, error_message)
    
    # Hiển thị hộp thoại lỗi
    show_error_diaglog(window_name, title, f"{expection_type}: {expection}\n{details}")