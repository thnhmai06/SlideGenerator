# Desciption: Logger Configuration
import os
import logging
import logging.handlers
from datetime import datetime
from colorama import Fore, init
from globals import DEBUG_MODE, LOG_PATH

# ? Configurations
init(autoreset=True)
BACKUP_COUNT = 50
log_filename = f"logs_{datetime.now().strftime('%Y-%m-%d_%H-%M-%S')}.log"
log_filepath = os.path.abspath(f"{LOG_PATH}/{log_filename}")

# If log_filepath does not exist, create it
if not os.path.exists(LOG_PATH):
    os.makedirs(LOG_PATH)

if DEBUG_MODE:
    level = logging.DEBUG
    formatter = logging.Formatter("<%(name)s> [%(levelname)s] %(message)s")
else:
    level = logging.INFO
    formatter = logging.Formatter("[%(levelname)s] %(message)s")

class ColorFormatter(logging.Formatter):
    COLOR_MAP = {
        "DEBUG": Fore.LIGHTBLACK_EX,
        "INFO": Fore.BLUE,
        "WARNING": Fore.YELLOW,
        "ERROR": Fore.RED,
        "CRITICAL": Fore.MAGENTA,
    }

    def format(self, record):
        # Sao chép giá trị gốc để tránh làm thay đổi record
        color = self.COLOR_MAP.get(record.levelname, "")
        record_copy = logging.makeLogRecord(record.__dict__)
        record_copy.msg = f"{color}{record.msg}{Fore.RESET}"
        record_copy.levelname = f"{color}{record.levelname}{Fore.RESET}"
        return super().format(record_copy)
color_formatter = ColorFormatter(formatter._fmt)

# ? File Handler
file_handler = logging.handlers.RotatingFileHandler(log_filepath, backupCount=BACKUP_COUNT, encoding="utf-8")
file_handler.setFormatter(formatter)
# ? Stream Handler (Console)
stream_handler = logging.StreamHandler()
stream_handler.setFormatter(color_formatter)

# Configure logging to use both handlers
logging.basicConfig(level=level, handlers=[stream_handler, file_handler])
