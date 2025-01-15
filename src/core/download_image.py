from typing import Callable
from classes.models import ProgressLogLevel
from src.utils.check_link import file as file_check, google_drive as GD_check, url as url_check
from src.utils.download import google_drive as GD_download, url as url_download 
from src.utils.file import copy_file
from globals import DOWNLOAD_PATH
import os


def __return_handler(add_log: Callable[[str, str, str, str], None], re: str | Exception | None, link: str):
    """
    Xử lý kết quả trả về từ các hàm tải xuống.
    Args:
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log progress.
        re (str | Exception | None): Kết quả trả về từ hàm tải xuống.
        link (str): Link của hình ảnh
    Returns:
        bool: True nếu thành công, ngược lại False.
    """
    if re is None:
        add_log(__name__, ProgressLogLevel.ERROR, "download_image_return_none", link)
        return False
    elif isinstance(re, Exception):
        add_log(__name__, ProgressLogLevel.ERROR, "download_image_return_exception", f"{re}")
        return False
    else:
        add_log(__name__, ProgressLogLevel.INFO, "download_image_success", f"{re}")
        return True

# ? Hàm chính
def download_image(link: str, num: int, add_log: Callable[[str, str, str, str], None]) -> str | None:
    """
    Tải hình ảnh từ link đã cho và lưu vào đường dẫn đã cho.
    Args:
        link (str): Link của hình ảnh.
        num (int): Số thứ tự của sinh viên.
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log progress.
    Returns:
        str: Đường dẫn tới file đã tải về nếu thành công, ngược lại None.
    """

    # Nếu link không được cung cấp, bỏ qua
    if not link:
        return None

    if file_check.is_image_file(link):
        # Là file ảnh trên hệ thống
        ext = link.split('.')[-1]
        return copy_file(link, f"{DOWNLOAD_PATH}/image_{num}.{ext}")
    elif url_check.is_url(link):
        # Là URL
        add_log(__name__, ProgressLogLevel.INFO, "download_image_start", link)
        if (ext := url_check.is_image_url(link)):
            # Là URL Ảnh
            save_path = os.path.abspath(f"{DOWNLOAD_PATH}/image_{num}.{ext}")
            re = url_download.download(link, save_path)
            return re if __return_handler(add_log, re, link) else None

        elif (GD_check.is_gd_url(link)):
            # Là URL Google Drive
            file_id = GD_check.get_file_id(link)
            view_link = GD_check.get_view_url(file_id)
            download_link = GD_check.get_download_url(file_id)
            if (ext := url_check.is_image_url(download_link)):
                # Là URL Google Drive Ảnh
                save_path = os.path.abspath(f"{DOWNLOAD_PATH}/image_{num}.{ext}")
                re = GD_download.download(view_link, save_path)
                return re if __return_handler(add_log, re, link) else None
            else:
                # URL không phải là Google Drive Ảnh
                add_log(__name__, ProgressLogLevel.INFO, "download_image_not_image", link)
                return None
        else:
            # Không phải là URL ảnh hay GD
            add_log(__name__, ProgressLogLevel.INFO, "download_image_not_image", link)
            return None
    else:
        # Không phải là gì hết
        add_log(__name__, ProgressLogLevel.INFO, "download_image_not_image", link)
        return None
    