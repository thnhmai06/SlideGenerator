import logging
import traceback
from src.ui import diaglogs
from translations import TRANS


def _show_err_diaglog(window_name: str, title: str, details: str = None):
    diaglogs.error(window_name, title, details)  # Show error dialog


def console_error(where: str, content: str) -> None:
    logger = logging.getLogger(where)
    if content:
        logger.error(content)


def _console_critical(where: str, content: str) -> None:
    logger = logging.getLogger(where)
    if content:
        logger.critical(content)


def default(
    where: str,
    title_key: str,
    details: str = None,
    window_name: str = TRANS["diaglogs"]["error"]["window_name"],
) -> None:
    """
    title_key: str - Tương ứng với key trong translation
    error_name: str - Lỗi là lỗi gì bên dưới title_key
    details: str - Chi tiết lỗi
    window_name: str - Tên của cửa sổ
    """
    title = TRANS["diaglogs"]["error"][title_key]
    console_error(where, details)
    _show_err_diaglog(window_name, title, details)


def exception(
    where: str,
    exctype: BaseException,
    value: str,
    tb: traceback.TracebackException,
    window_name: str = TRANS["diaglogs"]["error"]["window_name"]
    or "LANG is not found!",
) -> None:
    error_name = exctype.__name__
    title = f"{TRANS['diaglogs']['error']['exception']}\n\n{error_name}: {value}"
    details = "".join(traceback.format_tb(tb))
    _console_critical(where, f"{title}\n{details}")
    _show_err_diaglog(window_name, title, f"{error_name}: {value}\n{details}")
