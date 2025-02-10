import os
import logging
import logging.handlers
from colorama import Fore, init as colorama_init
from globals import DEBUG_MODE, LOG_PATH, OPEN_TIME

colorama_init(autoreset=True)
OPEN_TIME_FORMATTED = OPEN_TIME.strftime('%Y-%m-%d_%H-%M-%S')

class ColorFormatter(logging.Formatter):
    """
    Formatter cho phép hiển thị màu sắc trong console dựa trên mức độ log.
    """
    COLOR_MAP = {
        "DEBUG": Fore.LIGHTBLACK_EX,
        "INFO": Fore.BLUE,
        "WARNING": Fore.YELLOW,
        "ERROR": Fore.RED,
        "CRITICAL": Fore.MAGENTA,
    }

    def format(self, record):
        """
        Định dạng lại bản ghi log với màu sắc tương ứng.

        Args:
            record (LogRecord): Bản ghi log cần định dạng.

        Returns:
            str: Bản ghi log đã được định dạng.
        """
        color = self.COLOR_MAP.get(record.levelname, "")
        record_copy = logging.makeLogRecord(record.__dict__)
        record_copy.msg = f"{color}{record.msg}{Fore.RESET}"
        record_copy.levelname = f"{color}{record.levelname}{Fore.RESET}"
        return super().format(record_copy)

# ? Cấu hình
BACKUP_COUNT = 50
log_filename = f"logs_{OPEN_TIME_FORMATTED}.log"
log_filepath = os.path.abspath(f"{LOG_PATH}/{log_filename}")

# Nếu thư mục lưu log không tồn tại, tạo nó
os.makedirs(LOG_PATH, exist_ok=True)

# Chỉnh mức độ log thấp nhất và format dựa trên DEBUG_MODE có bật hay không
level = logging.DEBUG if DEBUG_MODE else logging.INFO
formatter = logging.Formatter("<%(name)s> [%(levelname)s] %(message)s" if DEBUG_MODE else "[%(levelname)s] %(message)s")
color_formatter = ColorFormatter(formatter._fmt)

# ? File Handler (ghi log ra file)
file_handler = logging.handlers.RotatingFileHandler(log_filepath, backupCount=BACKUP_COUNT, encoding="utf-8")
file_handler.setFormatter(formatter)

# ? Stream Handler (ghi log trên Console)
stream_handler = logging.StreamHandler()
stream_handler.setFormatter(color_formatter)

# Cấu hình logging để sử dụng cả hai handler
logging.basicConfig(level=level, handlers=[stream_handler, file_handler])
