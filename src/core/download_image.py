from typing import Callable, Type
from src.utils.check_link import file as file_check, google_drive as GD_check, url as url_check
from src.utils.download import google_drive as GD_download, url as url_download 
from globals import DOWNLOAD_PATH
import os


def __return_handler(add_log: Callable[[str, str, str, str], None], loglevel: Type, re: str | Exception | None, link: str):
    """
    Xử lý kết quả trả về từ các hàm tải xuống.
    Args:
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log progress.
        loglevel (Type): Level của log.
        re (str | Exception | None): Kết quả trả về từ hàm tải xuống.
        link (str): Link của hình ảnh
    Returns:
        bool: True nếu thành công, ngược lại False.
    """
    if re is None:
        add_log(__name__, loglevel.ERROR, "download_image_return_none", link)
        return False
    elif isinstance(re, Exception):
        add_log(__name__, loglevel.ERROR, "download_image_return_exception", f"{re}")
        return False
    else:
        add_log(__name__, loglevel.INFO, "download_image_success", f"{re}")
        return True

# ? Hàm chính
def download_image(link: str, num: int, add_log: Callable[[str, str, str, str], None], loglevel: Type) -> str | None:
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

    # Nếu link không được cung cấp, bỏ qua
    if not link:
        return None

    # Là file ảnh trên hệ thống
    if file_check.is_image_file(link):
        return link
    elif url_check.is_url(link):
        add_log(__name__, loglevel.INFO, "download_image_start", link)
        if (ext := url_check.is_image_url(link)):
            save_path = os.path.abspath(f"{DOWNLOAD_PATH}/image_{num}.{ext}")
            re = url_download.download(link, save_path)
            return re if __return_handler(add_log, loglevel, re, link) else None

        elif (GD_check.is_gd_url(link)):
            file_id = GD_check.get_file_id(link)
            raw_link = GD_check.get_raw_url(file_id)
            if (ext := url_check.is_image_url(raw_link)):
                save_path = os.path.abspath(f"{DOWNLOAD_PATH}/image_{num}.{ext}")
                re = GD_download.download(raw_link, save_path)
                return re if __return_handler(add_log, loglevel, re, link) else None
            else:
                # URL không phải là ảnh
                add_log(__name__, loglevel.INFO, "download_image_not_image", link)
                return None
        else:
            # Không phải là URL ảnh/GD
            add_log(__name__, loglevel.INFO, "download_image_not_image", link)
            return None
    else:
        # Không phải là gì hết
        add_log(__name__, loglevel.INFO, "download_image_not_image", link)
        return None
    