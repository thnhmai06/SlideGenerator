import logging
import traceback
from src.ui.diaglogs import error as show_error_diaglog
from translations import TRANS

def console_critical(where: str, content: str) -> None:
    """
    Ghi log lỗi nghiêm trọng ra console.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        content (str): Nội dung lỗi nghiêm trọng.
    """
    logger = logging.getLogger(where)
    if content:
        logger.critical(content)

def critical(
    where: str,
    expection: Exception,
    tb: traceback.TracebackException,
    window_name: str = TRANS["diaglogs"]["error"]["window_name"] or "LANG is not found!",
) -> None:
    """
    Ghi log lỗi nghiêm trọng (Expection xảy ra) và hiển thị hộp thoại lỗi.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        expection (Exception): Giá trị Expection.
        tb (traceback.TracebackException): Traceback của Expection.
        window_name (str, optional): Tên cửa sổ. Mặc định như trong file dịch (nếu không có sẽ là "LANG is not found!").
    """
    expection_type = expection.__class__.__name__
    title = f"{TRANS['diaglogs']['error']['exception']}\n\n{expection_type}: {expection}"
    details = "".join(traceback.format_tb(tb))
    console_critical(where, f"{title}\n{details}")
    show_error_diaglog(window_name, title, f"{expection_type}: {expection}\n{details}")