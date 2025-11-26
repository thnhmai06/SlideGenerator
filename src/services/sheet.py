#! Deprecated soon

from pathlib import Path

from src.core.service import TaskBasedService
from src.core.sheet import SheetGroup


class SheetService(TaskBasedService[Path, SheetGroup]):
    def open(self, file_path: Path) -> SheetGroup:
        """Open a sheet file and return a Group object representing its contents."""
        if file_path not in self._storage:
            self._storage[file_path] = SheetGroup(file_path)
        return self._storage[file_path]

    def close(self, file_path: Path):
        """Close the sheet file and remove it from storage."""
        if file_path in self._storage:
            del self._storage[file_path]

SERVICE = SheetService()