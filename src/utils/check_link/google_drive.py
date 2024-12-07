import re

GOOGLE_DRIVE_URL_PATTERN = r'^https?://drive\.google\.com/file/d/[^/]+'

def is_gd_url(url: str):
    """
    Kiểm tra xem url đã cho có phải link file Google Drive không.
    Args:
        url (str): URL cần kiểm tra.
    Returns:
        bool: True nếu là link file Google Drive, ngược lại False.
    """
    return re.match(GOOGLE_DRIVE_URL_PATTERN, url) is not None

def get_file_id(url: str):
    """
    Lấy ra ID của file từ link Google Drive.
    Args:
        url (str): URL của file Google Drive.
    Returns:
        str: ID của file nếu có, ngược lại None.
    """
    if is_gd_url(url):
        return url.split('/')[-2]
    return None

def get_raw_url(file_id: str):
    """
    Lấy ra URL trực tiếp của file từ ID của file Google Drive.
    Args:
        file_id (str): ID của file Google Drive.
    Returns:
        str: URL trực tiếp của file nếu có, ngược lại None.
    """
    return f"https://drive.google.com/uc?export=view&id={file_id}"