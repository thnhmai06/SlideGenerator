import logging
from ui import diaglogs
from translation import TRANS

def default(title_code: str, title_expection: str = "", details: str = None, window_name: str = TRANS["diaglogs"]["error"]["window_name"] or "Error") -> None:
    title = TRANS["diaglogs"]["error"][title_code] + "\n\n" + title_expection
    diaglogs.error(window_name, title, details) # Show error dialog
    logging.error(details) # Log error details

def expection(err: Exception, details: str, title: str = TRANS["diaglogs"]["error"]["expection"]) -> None:
    default("expection", title_expection=type(err).__name__ + ": " + str(err), details=details)
