import logging
from ui import diaglogs
from translation import TRANS

def _console_info(*contents: str) -> None:
    if contents: logging.info(" ".join(contents))
def _show_info_diaglog(window_name: str, title: str) -> None:
    diaglogs.info(window_name, title) # Show error dialog

def default(title_key: str, window_name: str = TRANS["diaglogs"]["information"]["window_name"]):
    """
    title_key: str - Tương ứng với key trong translation
    window_name: str - Tên của cửa sổ
    """
    title = TRANS["diaglogs"]["information"][title_key]
    _console_info(title)
    _show_info_diaglog(window_name, title)
