#! Deprecated soon

from pathlib import Path

from src.config import CONFIG
from src.models.download import DownloadTask, ImageDownloadTask
from src.models.service import TaskBasedService


class DownloadService(TaskBasedService[int, DownloadTask]):
    def __init__(self, save_folder: Path):
        """Initialize the download service with a save folder."""
        super().__init__()
        self._save_folder = save_folder
        self._id_counter = 0

    def create_image_task(self, image_url: str) -> int:
        """Create a new image download task for the given URL."""
        task = ImageDownloadTask(
            url=image_url,
            save_path=self._save_folder / f"{self._id_counter}"
        )
        self._storage[++self._id_counter] = task
        return self._id_counter

SERVICE = DownloadService(save_folder=CONFIG.save_folder)
