import logging
from ui import diaglogs
from translations import TRANS

def _console_info(where: str, *contents: str) -> None:
    __logger = logging.getLogger(where)
    if contents: 
        __logger.info(" ".join(contents))
def _show_info_diaglog(window_name: str, title: str) -> None:
    diaglogs.info(window_name, title) # Show error dialog

def default(where: str, title_key: str, window_name: str = TRANS["diaglogs"]["information"]["window_name"]):
    """
    title_key: str - Tương ứng với key trong translation
    window_name: str - Tên của cửa sổ
    """
    title = TRANS["diaglogs"]["information"][title_key]
    _console_info(where, title)
    _show_info_diaglog(window_name, title)
