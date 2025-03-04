from typing import Any
from src.ui.diaglogs import info as show_info_diaglog
from translations import get_text, format_text
from src.logging import info as log_info

def console_info(where: str, *contents: Any) -> None:
    """
    Ghi log thông tin ra console.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        *contents (Any): Các nội dung thông tin bổ sung cho log.
    """
    message = ' '.join(str(content) for content in contents)
    log_info(where, message)

def default(
    where: str,
    key: str,
    window_name: str = None,
    **format_args
) -> None:
    """
    Ghi log thông tin mặc định và hiển thị hộp thoại thông tin.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        key (str): Khóa tiêu đề trong file dịch.
        window_name (str, optional): Tên cửa sổ. Mặc định lấy từ file dịch.
        **format_args: Các tham số để định dạng văn bản.
    """
    # Lấy tiêu đề từ file dịch và định dạng nếu có tham số
    if format_args:
        contents = format_text(f"diaglogs.information.{key}", **format_args)
    else:
        contents = get_text(f"diaglogs.information.{key}")
    
    # Ghi log vào console
    console_info(where, contents)
    
    # Hiển thị hộp thoại thông tin
    show_info_diaglog(window_name, contents)
