import os
import logging
import logging.handlers
from colorama import Fore, init as colorama_init
from globals import DEBUG_MODE, LOG_PATH, OPEN_TIME

# Khởi tạo colorama
colorama_init(autoreset=True)

# Định dạng thời gian
TIME_FORMATTED = OPEN_TIME.strftime('%Y-%m-%d_%H-%M-%S')

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

# Cấu hình logging
BACKUP_COUNT = 50
log_filename = f"app_{TIME_FORMATTED}.log"
log_filepath = os.path.join(LOG_PATH, log_filename)

# Tạo thư mục log nếu chưa tồn tại
os.makedirs(LOG_PATH, exist_ok=True)

# Thiết lập mức độ log dựa trên chế độ debug
level = logging.DEBUG if DEBUG_MODE else logging.INFO

# Tạo formatter cho log
formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
color_formatter = ColorFormatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')

# File Handler (ghi log ra file)
file_handler = logging.handlers.RotatingFileHandler(
    log_filepath, 
    maxBytes=10*1024*1024,  # 10MB
    backupCount=BACKUP_COUNT, 
    encoding="utf-8"
)
file_handler.setFormatter(formatter)
file_handler.setLevel(level)

# Stream Handler (ghi log trên Console)
stream_handler = logging.StreamHandler()
stream_handler.setFormatter(color_formatter)
stream_handler.setLevel(level)

# Cấu hình root logger
root_logger = logging.getLogger()
root_logger.setLevel(level)

# Xóa các handler cũ nếu có
for handler in root_logger.handlers[:]:
    root_logger.removeHandler(handler)

# Thêm các handler mới
root_logger.addHandler(file_handler)
root_logger.addHandler(stream_handler)

# Singleton logger cho ứng dụng
_app_logger = None

def get_logger(name: str = None) -> logging.Logger:
    """
    Lấy logger cho module.

    Args:
        name (str, optional): Tên của module. Mặc định là None.

    Returns:
        logging.Logger: Logger cho module.
    """
    return logging.getLogger(name)

def setup_logger():
    """
    Thiết lập logger cho ứng dụng.
    """
    global _app_logger
    if _app_logger is None:
        _app_logger = logging.getLogger("app")
        _app_logger.setLevel(level)
        _app_logger.propagate = False
        
        # Thêm handlers nếu chưa có
        if not _app_logger.handlers:
            _app_logger.addHandler(file_handler)
            _app_logger.addHandler(stream_handler)
    
    return _app_logger

# Các hàm tiện ích để ghi log
def debug(where: str, message: str) -> None:
    """
    Ghi log với cấp độ DEBUG.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        message (str): Nội dung thông báo.
    """
    logger = get_logger(where)
    logger.debug(message)

def info(where: str, message: str) -> None:
    """
    Ghi log với cấp độ INFO.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        message (str): Nội dung thông báo.
    """
    logger = get_logger(where)
    logger.info(message)

def warning(where: str, message: str) -> None:
    """
    Ghi log với cấp độ WARNING.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        message (str): Nội dung thông báo.
    """
    logger = get_logger(where)
    logger.warning(message)

def error(where: str, message: str) -> None:
    """
    Ghi log với cấp độ ERROR.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        message (str): Nội dung thông báo.
    """
    logger = get_logger(where)
    logger.error(message)

def critical(where: str, message: str) -> None:
    """
    Ghi log với cấp độ CRITICAL.

    Args:
        where (str): Tên module hoặc vị trí gọi hàm.
        message (str): Nội dung thông báo.
    """
    logger = get_logger(where)
    logger.critical(message)

# Khởi tạo logger khi import module
setup_logger()
