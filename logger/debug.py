import logging
from ui import diaglogs
from translations import TRANS

def default(where: str, title_key: str, *details: str) -> None:
    title = TRANS["debug"][title_key]
    content = f"{title + '\n\n'.join(details)}"
    __logger = logging.getLogger(where)
    __logger.debug(content)
