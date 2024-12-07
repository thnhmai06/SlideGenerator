import gdown

def download(url: str, output_path: str) -> str | None | Exception:
    """
    Tải hình ảnh từ Google Drive xuống.
    Args:
        url (str): URL của file Google Drive.
        output (str): Đường dẫn để lưu file tải về (chứa cả tên file)
    Returns:
        str: Đường dẫn tới file đã tải về nếu thành công.
        None: Nếu không tải được.
        Exception: Nếu có lỗi xảy ra.
    """
    try:
        return gdown.download(url, output_path, quiet=False)
    except Exception as e:
        return e