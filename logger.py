import logging
from ui import diaglogs

def error(window_name: str, title: str, details: str) -> None:
    diaglogs.error(window_name, title, details) # Show error dialog
    logging.error(details) # Log error details