import logging
from src.ui.diaglogs import error as show_error_diaglog
from translations import TRANS

def console_error(where: str, content: str) -> None:
    """
    Ghi log lỗi ra console.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        content (str): Nội dung lỗi.
    """
    logger = logging.getLogger(where)
    if content:
        logger.error(content)

def default(
    where: str,
    title_key: str,
    details: str = None,
    window_name: str = TRANS["diaglogs"]["error"]["window_name"],
) -> None:
    """
    Ghi log lỗi mặc định và hiển thị hộp thoại lỗi.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        title_key (str): Khóa tiêu đề trong file dịch.
        details (str, optional): Chi tiết lỗi. Mặc định là None.
        window_name (str, optional): Tên cửa sổ. Mặc định như trong file dịch quy định.
    """
    title = TRANS["diaglogs"]["error"][title_key]
    console_error(where, f"{title}{'\n' if details else ''}{details or ''}")
    show_error_diaglog(window_name, title, details)
