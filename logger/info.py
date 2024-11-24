import logging
from ui import diaglogs
from translations import TRANS


def console_info(where: str, *contents: str) -> None:
    logger = logging.getLogger(where)
    if contents:
        logger.info(" ".join(contents))


def show_info_diaglog(window_name: str, title: str) -> None:
    diaglogs.info(window_name, title)  # Show error dialog


def default(
    where: str,
    title_key: str,
    window_name: str = TRANS["diaglogs"]["information"]["window_name"],
):
    """
    title_key: str - Tương ứng với key trong translation
    window_name: str - Tên của cửa sổ
    """
    title = TRANS["diaglogs"]["information"][title_key]
    console_info(where, title)
    show_info_diaglog(window_name, title)
