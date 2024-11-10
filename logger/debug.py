import logging
from ui import diaglogs
from translations import TRANS

def default(where: str, content_key: str):
    content = TRANS["debug"][content_key]
    __logger = logging.getLogger(where)
    __logger.debug(content)
