from pathlib import Path

from src.models.sheet import Group
from src.models.service import StorageService

class SheetService(StorageService[Path, Group]):
    def get(self, file_path: Path) -> Group | None:
        """Retrieve the Group object for the given file path if it exists."""
        return self._storage.get(file_path, None)

    def open(self, file_path: Path) -> Group:
        """Open a sheet file and return a Group object representing its contents."""
        if file_path not in self._storage:
            self._storage[file_path] = Group(file_path)
        return self._storage[file_path]

    def close(self, file_path: Path):
        """Close the sheet file and remove it from storage."""
        if file_path in self._storage:
            del self._storage[file_path]

SERVICE = SheetService()