import re
import requests
from src.logging.error import console_error
from globals import TIMEOUT

URL_PATTERN = r'^(https?|ftp):\/\/[a-zA-Z0-9.-]+(:\d+)?(\/[^\s]*)?$'
IMAGE_EXTENSIONS = ('png', 'jpg', 'jpeg', 'gif', 'bmp', 'tiff', 'svg')

def is_url(url: str):
    """
    Kiểm tra xem url đã cho có phải là url hợp lệ không.
    Args:
        url (str): URL cần kiểm tra.
    Returns:
        bool: True nếu là url hợp lệ, ngược lại False.
    """
    return (url is not None)  and (isinstance(url, str)) and (re.match(URL_PATTERN, url) is not None)

def is_image_url(url: str):
    """
    Kiểm tra xem url đã cho có phải là url của ảnh không và trả về phần mở rộng của file
    Args:
        url (str): URL cần kiểm tra.
    Returns:
        str: Phần mở rộng của file ảnh nếu là url của ảnh, ngược lại None.
    """
    # if not is_url(url):
    #     return None
    
    try:
        response = requests.head(url, allow_redirects=True, timeout=TIMEOUT, stream=True)
        content_type = response.headers.get('Content-Type')
        content_extension = content_type.split('/')[1]
        if content_type and content_type.startswith('image/') and content_extension in IMAGE_EXTENSIONS:
            return content_extension
    except requests.RequestException:
        return None
    except Exception as e:
        console_error(__name__, str(e))
        return None
    return None
    
