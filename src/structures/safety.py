"""Thread-safe implementation."""

from threading import RLock
from typing import Dict, Optional


class SafeDict[K, V]:
    """
    A simple thread-safe dictionary implementation.
    """

    def __init__(self):
        self._data: Dict[K, V] = {}
        self._lock = RLock()

    def __setitem__(self, key: K, value: V) -> None:
        with self._lock:
            self._data[key] = value

    def __delitem__(self, key: K) -> None:
        with self._lock:
            del self._data[key]

    def __getitem__(self, key: K) -> V:
        with self._lock:
            return self._data[key]

    def get(self, key: K, default: Optional[V] = None) -> V:
        with self._lock:
            return self._data.get(key, default)

    def pop(self, key: K, default: Optional[V] = None) -> V:
        with self._lock:
            return self._data.pop(key, default)

    def keys(self):
        with self._lock:
            return self._data.keys()
