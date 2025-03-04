import requests
import validators
from typing import Optional
from classes.models import ProgressLogLevel
from src.logging.error import console_error
from src.utils.retry import retry_with_exponential_backoff
from globals import TIMEOUT, IMAGE_EXTENSIONS
from src.ui.progress import log_progress
from classes.expections import RateLimitException

def _attempt_callback(attempt: int, retries: int):
    log_progress(__name__, ProgressLogLevel.INFO, "retry.attempt", attempt=attempt, retries=retries)

def _cooldown_callback(delay: float):
    log_progress(__name__, ProgressLogLevel.INFO, "retry.cooldown", delay=round(delay, 2))

def is_url(url: str) -> bool:
    """
    Kiểm tra xem url đã cho có phải là url hợp lệ không.

    Args:
        url (str): URL cần kiểm tra.

    Returns:
        bool: True nếu là url hợp lệ, ngược lại False.
    """
    # Kiểm tra
    if validators.url(url):
        return True
    return False

@retry_with_exponential_backoff(
    on_attempt=_attempt_callback, 
    on_cooldown=_cooldown_callback,
    exception_types=(RateLimitException,)
)
def get_image_extension(url: str) -> Optional[str]:
    """
    Kiểm tra xem url đã cho có phải là url của ảnh không và trả về phần mở rộng của file.

    Args:
        url (str): URL cần kiểm tra.

    Returns:
        Optional[str]: Phần mở rộng của file nếu là ảnh, ngược lại None.
    """
    try:
        response = requests.head(url, allow_redirects=True, timeout=TIMEOUT, stream=True)
        
        if RateLimitException.is_rate_limited(response):
            raise RateLimitException
            
        content_type = response.headers.get('Content-Type')
        content_disposition = response.headers.get('Content-Disposition')
        if content_disposition:
            extension = content_disposition.split('.')[-1].strip('"')
        elif content_type and content_type.startswith('image/'):
            extension = content_type.split('/')[1]
        else:
            return None

        extension = extension.lower()
        if extension in IMAGE_EXTENSIONS:
            return extension
        return None

    except RateLimitException:
        # Re-raise để kích hoạt retry
        raise
    except requests.exceptions.Timeout:
        log_progress(__name__, ProgressLogLevel.ERROR, "validate.timeout", time=TIMEOUT)
        return None
    except requests.RequestException as e:
        log_progress(__name__, ProgressLogLevel.ERROR, "validate.connection_error", error=str(e))
        return None
    except Exception as e:
        console_error(__name__, str(e))
        return None
