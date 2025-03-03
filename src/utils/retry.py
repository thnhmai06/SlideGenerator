import time
import random
from typing import TypeVar, Callable, Any, Optional
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
    jitter: bool = True
) -> Callable:
    """
    Decorator để thực hiện lại một hàm với thuật toán chờ luỹ thừa khi có lỗi.
    
    Args:
        on_attempt (Callable): Hàm callback được gọi trước mỗi lần thử lại. (int attempt, int retries)
        on_cooldown (Callable): Hàm callback được gọi trước mỗi lần chờ. (float delay)
        retries (int): Số lần thử lại.
        base_delay (float): Thời gian chờ cơ bản (giây).
        max_delay (float): Thời gian chờ tối đa (giây).
        backoff_factor (float): Hệ số mũ cho thời gian chờ.
        jitter (bool): Có thêm nhiễu ngẫu nhiên vào thời gian chờ hay không.
    
    Returns:
        Callable: Decorator đã được cấu hình.
    """
    def decorator(func: Callable[..., T]) -> Callable[..., Optional[T]]:
        @wraps(func)
        def wrapper(*args: Any, **kwargs: Any) -> Optional[T]:
            # Thực thi lần đầu
            result = func(*args, **kwargs)
            
            # Nếu thành công, trả về kết quả
            if result is not None and not isinstance(result, Exception):
                return result
                
            # Thực hiện thử lại nếu thất bại
            for attempt in range(1, retries + 1):
                # Tính thời gian chờ theo thuật toán luỹ thừa
                delay = min(base_delay * (backoff_factor ** attempt), max_delay)
                
                # Thêm nhiễu (jitter) nếu cần
                if jitter:
                    delay = delay * (0.5 + random.random())
                    
                # Chờ trước khi thử lại
                if on_cooldown is not None:
                    on_cooldown(delay)
                time.sleep(delay)
                
                # Thử lại thực thi hàm
                if on_attempt is not None:
                    on_attempt(attempt, retries)
                result = func(*args, **kwargs)
                
                # Nếu thành công, trả về kết quả
                if result is not None and not isinstance(result, Exception):
                    return result
                    
            # Trả về kết quả cuối cùng nếu tất cả các lần thử đều thất bại
            return result
            
        return wrapper
    return decorator
