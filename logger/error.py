import logging
from ui import diaglogs
from translation import TRANS

def _console_error(*content: str) -> None:
    if content: logging.error(content)
def _show_err_diaglog(window_name: str, title: str, details: str = None):
    diaglogs.error(window_name, title, details) # Show error dialog

def default(title_key: str, error_name: str = "", details: str = None, window_name: str = TRANS["diaglogs"]["error"]["window_name"] or "Error") -> None:
    """
    title_key: str - Tương ứng với key trong translation
    error_name: str - Lỗi là lỗi gì bên dưới title_key
    details: str - Chi tiết lỗi
    window_name: str - Tên của cửa sổ
    """
    title = TRANS["diaglogs"]["error"][title_key] + "\n\n" + error_name
    _console_error(details)
    _show_err_diaglog(title, details, window_name)

def expection(err: Exception, details: str, title: str = TRANS["diaglogs"]["error"]["expection"]) -> None:
    default("expection", error_name=type(err).__name__ + ": " + str(err), details=details)
