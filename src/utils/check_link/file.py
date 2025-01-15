import os
from globals import IMAGE_EXTENSIONS

def is_file(path: str):
    """
    Kiểm tra xem path đã cho có phải là một đường dẫn tới file trên máy không.
    Args:
        path (str): Đường dẫn cần kiểm tra.
    Returns:
        bool: True nếu là đường dẫn tới file trên máy, ngược lại False.
    """
    return (path is not None) and os.path.isfile(path)

def is_image_file(path: str):
    """
    Kiểm tra xem path đã cho có phải là file ảnh trên máy không.
    Args:
        path (str): Đường dẫn cần kiểm tra.
    Returns:
        bool: True nếu là file ảnh trên máy, ngược lại False.
    """
    return is_file(path) and path.lower().endswith(IMAGE_EXTENSIONS)
