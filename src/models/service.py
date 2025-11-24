from src.structures.safety import SafeDict


class StorageService[K, V]:
    def __init__(self):
        self._storage: SafeDict[K, V] = SafeDict()
