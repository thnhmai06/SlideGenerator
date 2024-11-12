import logging
from ui import diaglogs
from translations import TRANS

def console_debug(where: str, title_key: str, *details: str) -> None:
    title = TRANS["console"]["debug"][title_key]
    content = f"{title + '\n\n'.join(details)}"
    logger = logging.getLogger(where)
    logger.debug(content)
