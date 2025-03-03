import os
import re
from typing import Optional, Union
from classes.models import ProgressLogLevel
from src.utils.validate import (
    local_file as local_check,
    google_drive as GD_check,
    url as url_check
)
from src.utils.download import url
from globals import DOWNLOAD_PATH
from src.utils.file import copy_file
from src.ui.progress import log_progress

# Type định nghĩa cho kết quả tải xuống
DownloadResult = Union[str, Exception, None]

def _handle_download_result(result: DownloadResult, link: str) -> bool:
    """
    Xử lý kết quả trả về từ các hàm tải xuống.
    
    Args:
        result (DownloadResult): Kết quả trả về từ hàm tải xuống.
        link (str): Link của hình ảnh
        
    Returns:
        bool: True nếu tải xuống thành công, False nếu thất bại.
    """
    if result is None:
        log_progress(__name__, ProgressLogLevel.ERROR, "download_image.failed", url=link)
        return False
    
    if isinstance(result, Exception):
        log_progress(__name__, ProgressLogLevel.ERROR, "download_image.return_exception", error=str(result))
        return False
    
    log_progress(__name__, ProgressLogLevel.INFO, "download_image.success", path=result)
    return True

def _download_from_url(link: str, num: int) -> Optional[str]:
    """
    Tải xuống hình ảnh từ URL.
    
    Args:
        link (str): URL của hình ảnh.
        num (int): Số thứ tự của sinh viên.
        
    Returns:
        Optional[str]: Đường dẫn đến hình ảnh đã tải xuống, None nếu thất bại.
    """
    
    # Kiểm tra xem URL có phải là hình ảnh không
    ext = url_check.get_image_extension(link)
    if not ext:
        # Hiển thị thông báo lỗi nếu không phải là hình ảnh
        log_progress(__name__, ProgressLogLevel.INFO, "download_image.failed", url=link)
        return None
        
    # Tải xuống hình ảnh
    log_progress(__name__, ProgressLogLevel.INFO, "download_image.start", url=link)
    file_name = f"image_{num}.{ext}"
    file_path = os.path.join(DOWNLOAD_PATH, file_name)
    file_path = os.path.abspath(file_path)
    result = url.download(link, file_path)
    
    # Xử lý kết quả
    if _handle_download_result(result, link):
        return file_path
    
    return None

def _download_from_google_drive(link: str, num: int) -> Optional[str]:
    """
    Tải xuống hình ảnh từ Google Drive.
    
    Args:
        link (str): URL của file Google Drive.
        num (int): Số thứ tự của sinh viên.
        
    Returns:
        Optional[str]: Đường dẫn đến hình ảnh đã tải xuống, None nếu thất bại.
    """
    # Lấy ID của file
    file_id = GD_check.get_file_id_from_google_drive_url(link)
    if not file_id:
        # Hiển thị thông báo lỗi nếu không lấy được ID
        log_progress(__name__, ProgressLogLevel.INFO, "download_image.failed", url=link)
        return None
    
    download_link = GD_check.get_download_url(file_id)
    
    # Kiểm tra xem file có phải là hình ảnh không
    ext = url_check.get_image_extension(download_link)
    if not ext:
        # Hiển thị thông báo lỗi nếu không phải là hình ảnh
        log_progress(__name__, ProgressLogLevel.INFO, "download_image.failed", url=link)
        return None
        
    # Tải xuống hình ảnh
    log_progress(__name__, ProgressLogLevel.INFO, "download_image.start", url=link)
    file_name = f"image_{num}.{ext}"
    file_path = os.path.join(DOWNLOAD_PATH, file_name)
    file_path = os.path.abspath(file_path)
    result = url.download(download_link, file_path)
    
    # Xử lý kết quả
    if _handle_download_result(result, link):
        return file_path
    
    return None

def download_image(link: str, num: int) -> Optional[str]:
    """
    Tải xuống hình ảnh từ link.
    
    Args:
        link (str): Link của hình ảnh.
        num (int): Số thứ tự của sinh viên.
        
    Returns:
        Optional[str]: Đường dẫn đến hình ảnh đã tải xuống, None nếu thất bại.
    """
    # Nếu link trống
    if not link or link.strip() == "":
        log_progress(__name__, ProgressLogLevel.INFO, "download_image.no_link", student_num=num)
        return None
    
    # Nếu link là đường dẫn file
    if local_check.is_image_file(link):
        # Sao chép file vào thư mục tạm
        ext = link.split('.')[-1]
        file_name = f"image_{num}.{ext}"
        file_path = os.path.join(DOWNLOAD_PATH, file_name)
        return copy_file(link, file_path)
    
    # Kiểm tra xem URL đã có giao thức chưa
    has_protocol = re.match(r"^[a-zA-Z]+://", link)
    # Thêm giao thức https nếu chưa có giao thức
    if not has_protocol:
        link = "https://" + link
    
    log_progress(__name__, ProgressLogLevel.INFO, "download_image.check_vaild", url=link)
    # Nếu link không hợp lệ
    if not url_check.is_url(link):
        log_progress(__name__, ProgressLogLevel.INFO, "download_image.invalid_url", url=link)
        return None
    # Nếu link là Google Drive
    if GD_check.is_google_drive_url(link):
        return _download_from_google_drive(link, num)
    
    # Nếu link là URL
    return _download_from_url(link, num)