import re
import os
import requests
import gdown
from typing import Callable, Type
from src.core._utils import delete_all_file

VALID_IMAGE_EXTENSIONS = ('.png', '.jpg', '.jpeg', '.gif', '.bmp', '.tiff', '.webp', '.svg')

# ? Các hàm liên quan đến Local File
def _is_valid_file(path: str):
    """
    Kiểm tra xem path đã cho có phải là một đường dẫn hợp lệ không.
    Args:
        path (str): Đường dẫn cần kiểm tra.
    Returns:
        bool: True nếu là đường dẫn hợp lệ, ngược lại False.
    """
    return os.path.isfile(path)

def _is_image_file(path: str):
    """
    Kiểm tra xem path đã cho có phải là file ảnh không.
    Args:
        path (str): Đường dẫn cần kiểm tra.
    Returns:
        bool: True nếu là file ảnh, ngược lại False.
    """
    return path.lower().endswith(VALID_IMAGE_EXTENSIONS)

def _is_valid_image_file(path: str):
    """
    Kiểm tra xem path đã cho có phải là một đường dẫn hợp lệ và là file ảnh không.
    Args:
        path (str): Đường dẫn cần kiểm tra.
    Returns:
        bool: True nếu là đường dẫn hợp lệ và là file ảnh, ngược lại False.
    """
    return _is_valid_file(path) and _is_image_file(path)

# ? Các hàm liên quan đến Google Drive
def _is_google_drive_file_url(url: str):
    """
    Kiểm tra xem url đã cho có phải link file Google Drive không.
    Args:
        url (str): URL cần kiểm tra.
    Returns:
        bool: True nếu là link file Google Drive, ngược lại False.
    """
    GOOGLE_DRIVE_FILE_PATTERN = r'^https?://drive\.google\.com/file/d/[^/]+'
    return re.match(GOOGLE_DRIVE_FILE_PATTERN, url) is not None

def _download_file_from_google_drive(url: str, output: str, add_log: Callable[[str, str, str, str], None], loglevel: Type):
    """
    Tải hình ảnh từ Google Drive xuống.
    Args:
        url (str): URL của file Google Drive.
        output (str): Đường dẫn để lưu file tải về.
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log progress.
        loglevel (Type): Level của log.
    Returns:
        bool: True nếu tải thành công, ngược lại False.
    """
    try:
        add_log(__name__, loglevel.info, "download_image_start", url)
        gdown.download(url, output, quiet=False)
        add_log(__name__, loglevel.info, "download_image_finish", output)
        return True
    except Exception as e:
        add_log(__name__, loglevel.error, "download_image_error", str(e))
        return False
    
# ? Các hàm liên quan đến URL
def _is_valid_url(url: str):
    """
    Kiểm tra xem url đã cho có phải là một url hợp lệ không.
    Args:
        url (str): URL cần kiểm tra.
    Returns:
        bool: True nếu là url hợp lệ, ngược lại False.
    """
    URL_PATTERN = re.compile(
        r'^(?:http|ftp)s?://'  # http:// hoặc https://
        r'(?:(?:[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?\.)+(?:[A-Z]{2,6}\.?|[A-Z0-9-]{2,}\.?)|'  # domain...
        r'localhost|'  # localhost...
        r'\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|'  # ...hoặc một địa chỉ IP
        r'\[?[A-F0-9]*:[A-F0-9:]+\]?)'  # ...hoặc một địa chỉ IPv6
        r'(?::\d+)?'  # cổng tùy chọn
        r'(?:/?|[/?]\S+)$', re.IGNORECASE)
    return re.match(URL_PATTERN, url) is not None

def _get_image_filename_from_url(url: str, add_log: Callable[[str, str, str, str], None], loglevel: Type):
    """
    Kiểm tra xem link đã cho có phải là file ảnh không và trả về tên file ảnh.
    Args:
        url (str): URL cần kiểm tra.
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log progress.
        loglevel (Type): Level của log.
    Returns:
        str: Tên file ảnh nếu là file ảnh, ngược lại None.
    """
    try:
        response = requests.head(url)
        if response.status_code == 200:
            content_type = response.headers.get('Content-Type')
            if content_type and content_type.startswith('image/'):
                filename = os.path.basename(url)
                if filename.lower().endswith(VALID_IMAGE_EXTENSIONS):
                    return filename
                else: 
                    add_log(__name__, loglevel.error, "download_image_not_image", url)
    except Exception:
        add_log(__name__, loglevel.error, "download_image_cant_access", url)
        return None

def _download_file_from_url(url: str, output: str, add_log: Callable[[str, str, str, str], None], loglevel: Type):
        """
        Tải file từ URL đã cho và lưu vào đường dẫn đã cho.
        Args:
            url (str): URL của file.
            output (str): Đường dẫn để lưu file tải về.
            add_log (Callable[[str, str, str, str], None]): Hàm ghi log progress.
            loglevel (Type): Level của log.
        Returns:
            bool: True nếu tải thành công, ngược lại False.
        """
        try:
            add_log(__name__, loglevel.info, "download_image_start", url)
            response = requests.get(url)
            if response.status_code == 200:
                with open(output, 'wb') as f:
                    f.write(response.content)
                add_log(__name__, loglevel.info, "download_image_finish", output)
                return True
        except Exception as e:
            add_log(__name__, loglevel.error, "download_image_error", str(e))
        return False

# ? Hàm chính
def download_image(link: str, save_path: str, add_log: Callable[[str, str, str, str], None], loglevel: Type):
    """
    Tải hình ảnh từ link đã cho và lưu vào đường dẫn đã cho.
    Args:
        link (str): Link của hình ảnh.
        save_path (str): Đường dẫn để lưu file nếu tải về.
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log progress.
        loglevel (Type): Level của log.
    Returns:
        str: Đường dẫn tới file đã tải về nếu thành công, ngược lại None.
    """
    # Tạo folder nếu thư mục lưu không tồn tại
    if not os.path.exists(save_path):
        os.makedirs(save_path)

    # Xóa hết các file trong save_path
    delete_all_file(save_path)

    # Nếu link đã cho là file ảnh trên máy và hợp lệ
    if _is_valid_image_file(link):
        return link
    
    # Nếu link không phải là url hợp lệ
    if not _is_valid_url(link):
        add_log(__name__, loglevel.info, "download_image_invalid_url", link)
        return None

    filename = str()
    if _is_google_drive_file_url(link):
        # Nếu link là link file Google Drive
        RAW_GOOGLE_DRIVE_FILE_HEADER = 'https://drive.google.com/uc?export=view&id='
        file_id = link.split('/d/')[1].split('/')[0]
        raw_google_drive_url = f"{RAW_GOOGLE_DRIVE_FILE_HEADER}{file_id}"

        filename = _get_image_filename_from_url(raw_google_drive_url, add_log, loglevel)
        if filename:
            output_path = os.path.join(save_path, filename)
            if _download_file_from_google_drive(raw_google_drive_url, output_path, add_log, loglevel):
                return output_path
        return None
    else:
        filename = _get_image_filename_from_url(link, add_log, loglevel)
        if filename:
            output_path = os.path.join(save_path, filename)
            if _download_file_from_url(link, output_path, add_log, loglevel):
                return output_path

        return None