import logging
from ui import diaglogs
from translation import TRANS

def default(title_code: str, window_name: str = TRANS["diaglogs"]["information"]["window_name"]):
    title = TRANS["diaglogs"]["information"][title_code]
    diaglogs.info(window_name, title) # Show error dialog
    logging.info(title) # Log error details
