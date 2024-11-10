import logging
from ui import diaglogs
from translations import TRANS

def _show_err_diaglog(window_name: str, title: str, details: str = None):
    diaglogs.error(window_name, title, details) # Show error dialog
def _console_error(where: str, content: str) -> None:
    __logger = logging.getLogger(where)
    if content: 
        __logger.error(content)
def _console_critical(where: str, content: str) -> None:
    __logger = logging.getLogger(where)
    if content:
        __logger.critical(content)

def default(where: str, title_key: str, details: str = None, window_name: str = TRANS["diaglogs"]["error"]["window_name"]) -> None:
    """
    title_key: str - Tương ứng với key trong translation
    error_name: str - Lỗi là lỗi gì bên dưới title_key
    details: str - Chi tiết lỗi
    window_name: str - Tên của cửa sổ
    """
    title = TRANS["diaglogs"]["error"][title_key]
    _console_error(where, details)
    _show_err_diaglog(title, details, window_name)
def expection(where: str, err: Exception, details: str, title: str = TRANS["diaglogs"]["error"]["expection"], window_name: str = TRANS["diaglogs"]["error"]["window_name"] or "LANG is not found!") -> None:
    error_name = err.__class__.__name__
    _console_critical(where, details)
    _show_err_diaglog(window_name, title + "\n\n" + error_name + ": " + str(err), details)