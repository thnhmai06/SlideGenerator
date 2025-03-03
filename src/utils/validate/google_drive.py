import re

GOOGLE_DRIVE_URL_PATTERN = re.compile(r'https?://drive\.google\.com/(?:file/d/|uc\?id=)([a-zA-Z0-9_-]+)')

def is_google_drive_url(url: str) -> bool:
    """
    Kiểm tra xem url đã cho có phải link file Google Drive không.

    Args:
        url (str): URL cần kiểm tra.

    Returns:
        bool: True nếu là link file Google Drive, ngược lại False.
    """
    if GOOGLE_DRIVE_URL_PATTERN.match(url):
        return True
    return False

def get_file_id_from_google_drive_url(url: str) -> str:
    """
    Lấy ra ID của file từ link Google Drive.

    Args:
        url (str): URL của file Google Drive.

    Returns:
        str: ID của file nếu có, ngược lại None.
    """
    match = GOOGLE_DRIVE_URL_PATTERN.search(url)
    if match:
        return match.group(1)
    return None

def get_download_url(file_id: str) -> str:
    """
    Lấy ra URL tải xuống của file từ ID của file Google Drive.

    Args:
        file_id (str): ID của file Google Drive.

    Returns:
        str: URL tải xuống của file.
    """
    return f"https://drive.google.com/uc?id={file_id}&export=download"

def get_view_url(file_id: str) -> str:
    """
    Lấy ra URL xem trước của file từ ID của file Google Drive.

    Args:
        file_id (str): ID của file Google Drive.

    Returns:
        str: URL xem trước của file.
    """
    return f"https://drive.google.com/uc?id={file_id}&export=view"