import time
import random
from typing import TypeVar, Callable, Any, Type, Tuple
from functools import wraps
from globals import TIMEOUT, RETRIES

T = TypeVar('T')

def retry_with_exponential_backoff(
    on_attempt: Callable[[int, int], None] = None,
    on_cooldown: Callable[[float], None] = None,
    retries: int = RETRIES,
    base_delay: float = 1.0,
    max_delay: float = TIMEOUT,
    backoff_factor: float = 2.0,
    jitter: bool = True,
    exception_types: Tuple[Type[Exception], ...] = (Exception,)
) -> Callable:
    """
    Decorator để thực hiện lại một hàm với thuật toán chờ luỹ thừa khi có lỗi.
    
    Args:
        on_attempt: Hàm callback được gọi trước mỗi lần thử lại. (int attempt, int retries)
        on_cooldown: Hàm callback được gọi trước mỗi lần chờ. (float delay)
        retries: Số lần thử lại.
        base_delay: Thời gian chờ cơ bản (giây).
        max_delay: Thời gian chờ tối đa (giây).
        backoff_factor: Hệ số mũ cho thời gian chờ.
        jitter: Có thêm nhiễu ngẫu nhiên vào thời gian chờ hay không.
        exception_types: Các loại ngoại lệ cần retry.
    
    Returns:
        Decorator đã được cấu hình.
    """
    def decorator(func: Callable[..., T]) -> Callable[..., T]:
        @wraps(func)
        def wrapper(*args: Any, **kwargs: Any) -> T:
            last_exception = None
            
            for attempt in range(retries + 1):  # +1 vì bao gồm cả lần chạy đầu tiên
                try:
                    if attempt > 0 and on_attempt:
                        on_attempt(attempt, retries)
                    return func(*args, **kwargs)
                    
                except exception_types as e:
                    last_exception = e
                    
                    # Nếu là lần cuối cùng, không cần chờ nữa
                    if attempt == retries:
                        break
                        
                    # Tính thời gian chờ
                    delay = min(base_delay * (backoff_factor ** attempt), max_delay)
                    if jitter:
                        delay = delay * (0.5 + random.random())
                        
                    # Thông báo và chờ
                    if on_cooldown:
                        on_cooldown(delay)
                    time.sleep(delay)
                    
                except Exception:
                    # Nếu không phải exception_types được chỉ định, raise ngay lập tức
                    raise
            
            # Nếu tất cả các lần thử đều thất bại
            if last_exception:
                raise last_exception
            
        return wrapper
    return decorator
