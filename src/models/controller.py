import threading
import time
from abc import abstractmethod, ABC
from datetime import datetime
from typing import Optional


class RetryController(ABC):
    """
    Manages retry logic for tasks with exponential backoff
    """

    def __init__(self, initial_delay: float, multiplier: float, max_delay: float, max_retries: int):
        """
        Initialize RetryController with backoff parameters

        Args:
            initial_delay (float): Initial delay before first retry in seconds
            multiplier (float): Multiplier for exponential backoff
            max_delay (float): Maximum delay between retries in seconds
            max_retries (int): Maximum number of retry attempts
        """
        self._count: int = 0
        self._last_retry_time: Optional[datetime] = None
        self._initial_delay: float
        self._multiplier: float
        self._max_delay: float
        self._max_retries: int

        self._initial_delay = initial_delay
        self._multiplier = multiplier
        self._max_delay = max_delay
        self._max_retries = max_retries

    @property
    def count(self) -> int:
        return self._count

    @property
    def last_retry_time(self) -> Optional[datetime]:
        return self._last_retry_time

    def get_retry_delay(self) -> float:
        """Calculate retry delay with exponential backoff"""
        delay = self._initial_delay * (
                self._multiplier ** self._count
        )
        return min(delay, self._max_delay)

    @staticmethod
    @abstractmethod
    def _check_retry(exception: Exception) -> bool:
        """
        Check if task should retry based on exception

        Args:
            exception (Exception): The exception raised during task execution
        Returns:
            bool: True if task should retry, False otherwise
        """
        pass

    def _should_retry(self, exception: Exception) -> bool:
        """Check if task should retry based on exception and retry count"""
        if self._count >= self._max_retries:
            return False

        return self._check_retry(exception)

    def make_retry(self, exception: Exception) -> float:
        """
        Make a retry attempt after checking conditions

        Args:
            exception (Exception): The exception raised during task execution
        Returns:
            float: The delay before the next retry
        """
        if not self._should_retry(exception):
            raise exception

        delay = self.get_retry_delay()
        time.sleep(delay)

        self._count += 1
        self._last_retry_time = datetime.now()
        return delay


class FlowController:
    def __init__(self, on_pause: Optional[callable] = None, on_resume: Optional[callable] = None):
        """
        Control flow of tasks with pause and resume functionality

        Args:
            on_pause (Optional[callable]): Callback function when paused
            on_resume (Optional[callable]): Callback function when resumed
        """
        self._lock = threading.Lock()
        self._condition = threading.Condition(self._lock)
        self._flag = False

        self.on_pause = on_pause
        self.on_resume = on_resume

    def pause(self):
        """Pause the flow"""
        with self._condition:
            self._flag = True
            if self.on_pause:
                self.on_pause()

    def resume(self):
        """Resume the flow"""
        with self._condition:
            self._flag = False
            self._condition.notify_all()
            if self.on_resume:
                self.on_resume()

    def wait_resume(self):
        """Wait until the flow is resumed"""
        with self._condition:
            while self._flag:
                self._condition.wait()

    @property
    def flag(self):
        return self._flag

    def __bool__(self):
        return self._flag
