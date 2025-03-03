from typing import Optional, Any
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
    window_name: Optional[str] = None,
    **format_args
) -> None:
    """
    Ghi log thông tin mặc định và hiển thị hộp thoại thông tin.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        key (str): Khóa tiêu đề trong file dịch.
        window_name (Optional[str], optional): Tên cửa sổ. Mặc định lấy từ file dịch.
        **format_args: Các tham số để định dạng văn bản.
    """
    # Lấy tiêu đề từ file dịch và định dạng nếu có tham số
    if format_args:
        title = format_text(f"diaglogs.information.{key}", **format_args)
    else:
        title = get_text(f"diaglogs.information.{key}")
    
    # Lấy tên cửa sổ từ file dịch nếu không được cung cấp
    if window_name is None:
        window_name = get_text('diaglogs.information.window_name')
    
    # Ghi log vào console
    console_info(where, title)
    
    # Hiển thị hộp thoại thông tin
    show_info_diaglog(window_name, title)
