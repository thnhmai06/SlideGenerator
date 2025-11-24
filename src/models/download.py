"""Download manager service for handling file downloads with retry and flow control"""

import os
import threading
from abc import ABC, abstractmethod
from concurrent.futures import ThreadPoolExecutor
from enum import Enum
from pathlib import Path
from queue import Queue
from typing import Dict

import requests
from requests import Response

from src.config import CONFIG, IMAGE_EXTENSIONS
from src.exceptions.file import FileExtensionNotSupported
from src.models.controller import RetryController, FlowController
from src.utils.http import get_file_extension


class DownloadStatus(Enum):
    """Enumeration of download statuses"""
    QUEUED = "queued"
    CONNECTING = "connecting"
    DOWNLOADING = "downloading"
    PAUSED = "paused"
    COMPLETED = "completed"
    ERROR = "error"
    STOPPED = "stopped"


class _DownloadTask(ABC):
    """Represents a download task with retry and flow control"""

    def __init__(self, url: str, save_path: Path):
        """
        Initialize download task
        Args:
            url (str): URL of the file to download
            save_path (Path): Path to save the downloaded file (without extension)
        """
        self._retry = _DownloadRetry()
        self._total_size = 0  # bytes
        self._lock = threading.Lock()
        self._controller = FlowController()

        self._url = url
        self._save_path = save_path
        self._extension = ''
        self._status = DownloadStatus.QUEUED
        self._stopped = False

    @staticmethod
    @abstractmethod
    def _check_response(response: Response) -> None:
        pass

    def stop(self):
        """Stop the download immediately"""
        self._stopped = True
        self._status = DownloadStatus.STOPPED
        if self._controller.flag:
            self._controller.resume()

    def download(self):
        """
        Download/Continue download file
        """
        with self._lock:
            while not self.completed and not self._stopped:
                try:
                    self._status = DownloadStatus.CONNECTING

                    headers = {}
                    mode = 'wb'
                    if (downloaded := self.downloaded_size) > 0:
                        headers = {'Range': f'bytes={downloaded}-'}
                        mode = 'ab'

                    if self._stopped: break

                    response = requests.get(
                        url=self.url,
                        headers=headers,
                        stream=True,
                        timeout=(CONFIG.download_timeout_connect, CONFIG.download_timeout_request)
                    )

                    try:
                        response.raise_for_status()
                        self._check_response(response)

                        # Get file extension
                        if not self._extension:
                            self._extension = get_file_extension(response)

                        # Get total size
                        content_length = int(response.headers.get('content-length', 0))
                        if response.status_code == 206:  # Support resuming
                            self._total_size = downloaded + content_length
                        else:
                            self._total_size = content_length
                            mode = 'wb'
                            # downloaded = 0  # reset because of rewriting

                        # Write to file
                        with open(self.save_file, mode) as file:
                            self._status = DownloadStatus.DOWNLOADING

                            for data in response.iter_content(chunk_size=8192):
                                self._controller.wait_resume()

                                if self._stopped:
                                    break

                                if data:
                                    file.write(data)
                    finally:
                        response.close()

                except Exception as e:
                    if self._stopped:
                        break

                    self._status = DownloadStatus.ERROR
                    self._retry.make_retry(e)

            if self.completed:
                self._status = DownloadStatus.COMPLETED

    @property
    def url(self) -> str:
        return self._url

    @property
    def status(self) -> DownloadStatus:
        if self._status in (DownloadStatus.CONNECTING, DownloadStatus.DOWNLOADING) and self._controller.flag:
            return DownloadStatus.PAUSED
        return self._status

    @property
    def total_size(self) -> int:
        return self._total_size

    @property
    def controller(self):
        """Get flow controller for pausing/resuming download"""
        return self._controller

    @property
    def save_file(self) -> Path:
        return Path(f"{self._save_path}.{self._extension}") if self._extension else self._save_path

    @property
    def downloaded_size(self) -> int:
        return os.path.getsize(self.save_file) if os.path.exists(self.save_file) else 0

    @property
    def progress(self) -> float:
        if self.total_size == 0:
            return 0
        return (self.downloaded_size / self.total_size) * 100

    @property
    def completed(self) -> bool:
        return 0 < self.total_size <= self.downloaded_size


class _ImageDownloadTask(_DownloadTask):
    """
    Download task for image files
    """

    def __init__(self, url: str, save_path: Path):
        super().__init__(url, save_path)

    @staticmethod
    def _check_response(response: Response) -> None:
        if (ext := get_file_extension(response)) not in IMAGE_EXTENSIONS:
            raise FileExtensionNotSupported(ext)


class _DownloadRetry(RetryController):
    """
    Manages retry logic for download tasks
    """

    def __init__(self):
        super().__init__(
            initial_delay=CONFIG.download_retry_initial_delay,
            multiplier=CONFIG.download_retry_multiplier,
            max_delay=CONFIG.download_retry_max_delay,
            max_retries=CONFIG.download_retry_max_retries
        )

    @staticmethod
    def _check_retry(exception: Exception) -> bool:
        if isinstance(exception, requests.RequestException):
            if hasattr(exception, 'response') and exception.response is not None:
                status_code = exception.response.status_code
                return status_code in CONFIG.download_retry_on_status_codes
            return True  # Network errors are retryable
        return False

# TODO: Cần viết lại DownloadManager để đồng bộ với task bên ngoài.
# class DownloadManager:
#     """Manages multiple download tasks with concurrency"""
#
#     def __init__(self, save_folder: Path, max_workers: int):
#         self._tasks: Queue[int] = Queue()
#         self._completed: Dict[int, _DownloadTask] = {}
#         self._workers = ThreadPoolExecutor(max_workers)
#         self._save_folder = save_folder
#         self._id_counter = 0
#
#         for _ in range_(max_workers):
#             self._workers.submit(self._worker)
#
#     @staticmethod
#     def _get_formatted_url(url: str) -> str:
#         return url.strip()
#
#     def _worker(self):
#         while True:
#             task_id = self._tasks.get()
#             task = self.get_task(task_id)
#
#             task.download()
#
#             if task.completed:
#                 del self._completed[task_id]
#             self._tasks.task_done()
#
#     def add_image_task(self, url: str) -> int:
#         """Add image download task"""
#         url = self._get_formatted_url(url)
#
#         task = _ImageDownloadTask(
#             url=url,
#             save_path=self._save_folder / f"{self._id_counter}"
#         )
#         self._tasks.put(task)
#         self._completed[++self._id_counter] = task
#         return self._id_counter
#
#     def remaining_amount(self) -> int:
#         """Get number of remaining download tasks"""
#         return self._tasks.qsize()
#
#     def get_task(self, id_: int) -> _DownloadTask:
#         """Get download task by ID"""
#         return self._completed.get(id_)
