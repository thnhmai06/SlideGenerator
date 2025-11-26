"""Module defining a generic task-based service class."""

from src.structures.safety import SafeDict


class TaskBasedService[K, V]:
    """A generic service class that manages tasks based on keys and values."""

    def __init__(self):
        self._storage: SafeDict[K, V] = SafeDict()

    def get(self, key: K) -> V | None:
        """Retrieve a value from storage by its key."""
        return self._storage.get(key, None)
