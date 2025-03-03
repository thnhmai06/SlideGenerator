import requests
from globals import TIMEOUT
from src.ui.progress import log_progress
from classes.models import ProgressLogLevel
from src.utils.retry import retry_with_exponential_backoff

def _attempt_callback(attempt: int, retries: int):
    """
    Callback được gọi trước mỗi lần thử lại.

    Args:
        attempt (int): Số lần thử lại.
        retries (int): Số lần thử lại tối đa.
    """
    log_progress(__name__, ProgressLogLevel.INFO, "retry.attempt", attempt=attempt, retries=retries)

def _cooldown_callback(delay: float):
    """
    Callback được gọi trước mỗi lần chờ.

    Args:
        delay (float): Thời gian chờ (giây).
    """
    log_progress(__name__, ProgressLogLevel.INFO, "retry.cooldown", delay=round(delay, 2))

@retry_with_exponential_backoff(on_attempt=_attempt_callback, on_cooldown=_cooldown_callback)
def download(url: str, output_path: str) -> str | None | Exception:
    """
    Tải hình ảnh từ url xuống.

    Args:
        url (str): URL của file.
        output_path (str): Đường dẫn để lưu file tải về.

    Returns:
        str: Đường dẫn tới file đã tải về nếu thành công.
        None: Nếu không tải được file.
        Exception: Nếu có lỗi xảy ra.
    """
    try:
        response = requests.get(url, allow_redirects=True, timeout=TIMEOUT, stream=True)
        if response.status_code == 200:
            with open(output_path, 'wb') as f:
                f.write(response.content)
            return output_path
        return None
    except requests.RequestException as e:
        log_progress(__name__, ProgressLogLevel.ERROR, "download_image.connection_error", error=str(e))
        return e
    except Exception as e:
        log_progress(__name__, ProgressLogLevel.ERROR, "download_image.return_exception", error=str(e))
        return e
