import logging
from translations import TRANS

def console_debug(where: str, title_key: str | None, *details: str) -> None:
    """
    Ghi log debug ra console.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        title_key (str | None): Khóa tiêu đề trong file dịch.
        *details (str): Các chi tiết bổ sung cho log.
    """
    title = TRANS["console"]["debug"][title_key] if title_key else ""
    content = f"{title + '\n\n'.join(details)}"
    logger = logging.getLogger(where)
    logger.debug(content)
