import requests
from globals import TIMEOUT

def download(url: str, output_path: str):
    """
    Tải hình ảnh từ url xuống.
    Args:
        url (str): URL của file.
        output (str): Đường dẫn để lưu file tải về.
    Returns:
        str: Đường dẫn tới file đã tải về nếu thành công, Exception: Nếu có lỗi xảy ra, None: Nếu không tải được file.
    """
    try:
        response = requests.get(url, allow_redirects=True, timeout=TIMEOUT, stream=True)
        if response.status_code == 200:
            with open(output_path, 'wb') as f:
                f.write(response.content)
            return output_path
    except Exception as e:
        return e 
