import os
import sys
from datetime import datetime
from PyQt5.QtWidgets import QApplication
from configparser import ConfigParser
from classes.models import Input

# Đọc file cấu hình
__CONFIG = ConfigParser()
__CONFIG.read("./settings.ini")
LANG = __CONFIG.get("Config", "lang")  # Ngôn ngữ của ứng dụng
TIMEOUT = __CONFIG.getfloat("Config", "timeout")  # Thời gian chờ cho các yêu cầu mạng
RETRIES = __CONFIG.getint("Config", "retry")  # Số lần thử lại khi có lỗi tải xuống
DEBUG_MODE = __CONFIG.getboolean("Debug", "debug")  # Chế độ gỡ lỗi có bật hay không

# ? Cấu hình
GITHUB_URL = "https://github.com/thnhmai06/tao-slide-tot-nghiep"  # URL của repository trên GitHub
TEMP_FOLDER = os.path.abspath("./temp/")  # Thư mục tạm thời
LOG_PATH = os.path.abspath("./logs/")  # Thư mục lưu trữ các tệp log
TRANSLATION_PATH = os.path.abspath("./translations/")  # Thư mục lưu trữ các tệp dịch
# IMAGE_EXTENSIONS = {'emf', 'wmf', 'jpg', 'jpeg', 'jfif', 'jpe', 'png', 'bmp', 'dib', 'rle', 'gif', 'emz', 'wmz', 'tif', 'tiff', 'svg', 'ico', 'heif', 'heic', 'hif', 'avif', 'webp'} # Powerpoint Support
IMAGE_EXTENSIONS = {'jpg', 'jpeg', 'jfif', 'jpe', 'png', 'bmp', 'dib', 'gif', 'tif', 'tiff', 'ico', 'heif', 'heic', 'avif', 'webp'}  # Các phần mở rộng tệp ảnh được hỗ trợ bởi Pillow

# ? Biến toàn cục
OPEN_TIME = datetime.now()  # Thời gian mở ứng dụng
TEMP_TIME_FOLDER = os.path.join(TEMP_FOLDER, OPEN_TIME.strftime('%Y-%m-%d_%H-%M-%S'))  # Thư mục tạm thời cho phiên làm việc hiện tại

# Các thư mục con trong thư mục thời gian
SHAPES_PATH = os.path.join(TEMP_TIME_FOLDER, "shapes")  # Đường dẫn lưu trữ các Shapes
DOWNLOAD_PATH = os.path.join(TEMP_TIME_FOLDER, "downloads")  # Đường dẫn lưu trữ các tệp tải xuống
PROCESSED_PATH = os.path.join(TEMP_TIME_FOLDER, "processed")  # Đường dẫn lưu trữ các tệp ảnh đã xử lý

app = QApplication(sys.argv)
user_input = Input()
