from typing import Optional, Any
from translations import get_text
from src.logging import debug as log_debug

def console_debug(where: str, key: Optional[str] = None, *details: Any) -> None:
    """
    Ghi log debug ra console.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        key (Optional[str]): Khóa tiêu đề trong file dịch.
        *details (Any): Các chi tiết bổ sung cho log.
    """
    if key:
        title = get_text(f"console.debug.{key}")
        content = f"{title}{' ' if details else ''}{' '.join(str(detail) for detail in details)}"
    else:
        content = ' '.join(str(detail) for detail in details)
    
    log_debug(where, content)
