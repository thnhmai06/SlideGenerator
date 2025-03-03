from typing import Optional
from src.ui.diaglogs import error as show_error_diaglog
from translations import get_text, format_text
from src.logging import error as log_error

def console_error(where: str, content: str) -> None:
    """
    Ghi log lỗi ra console.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        content (str): Nội dung lỗi.
    """
    log_error(where, content)

def default(
    where: str,
    key: str,
    details: Optional[str] = None,
    window_name: Optional[str] = None,
    **format_args
) -> None:
    """
    Ghi log lỗi mặc định và hiển thị hộp thoại lỗi.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        key (str): Khóa tiêu đề trong file dịch.
        details (Optional[str], optional): Chi tiết lỗi. Mặc định là None.
        window_name (Optional[str], optional): Tên cửa sổ. Mặc định lấy từ file dịch.
        **format_args: Các tham số để định dạng văn bản.
    """
    # Lấy tiêu đề từ file dịch và định dạng nếu có tham số
    if format_args:
        title = format_text(f"diaglogs.error.{key}", **format_args)
    else:
        title = get_text(f"diaglogs.error.{key}")
    
    # Lấy tên cửa sổ từ file dịch nếu không được cung cấp
    if window_name is None:
        window_name = get_text('diaglogs.error.window_name')
    
    # Ghi log vào console
    error_message = f"{title}{'\n' if details else ''}{details or ''}"
    console_error(where, error_message)
    
    # Hiển thị hộp thoại lỗi
    show_error_diaglog(window_name, title, details)
