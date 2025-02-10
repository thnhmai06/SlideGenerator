import logging
from src.ui.diaglogs import info as show_info_diaglog
from translations import TRANS

def console_info(where: str, *contents: str) -> None:
    """
    Ghi log thông tin ra console.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        *contents (str): Các nội dung thông tin bổ sung cho log.
    """
    logger = logging.getLogger(where)
    if contents:
        logger.info(' '.join(contents))

def default(
    where: str,
    title_key: str,
    window_name: str = TRANS["diaglogs"]["information"]["window_name"],
) -> None:
    """
    Ghi log thông tin mặc định và hiển thị hộp thoại thông tin.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        title_key (str): Khóa tiêu đề trong file dịch.
        window_name (str, optional): Tên cửa sổ. Mặc định trong file dịch quy định.
    """
    title = TRANS["diaglogs"]["information"][title_key]
    console_info(where, title)
    show_info_diaglog(window_name, title)
