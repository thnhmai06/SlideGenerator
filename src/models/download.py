"""Download manager service for handling file downloads with retry and flow control"""

import threading
from abc import ABC, abstractmethod
from enum import Enum
from pathlib import Path

import requests
from requests import Response

from src.config import CONFIG, IMAGE_EXTENSIONS
from src.models.exceptions import FileExtensionNotSupported
from src.models.controller import RetryController, FlowController
from src.utils.http import get_file_extension, correct_image_url


class DownloadStatus(Enum):
    """Enumeration of download statuses"""
    QUEUED = "queued"
    CONNECTING = "connecting"
    DOWNLOADING = "downloading"
    PAUSED = "paused"
    COMPLETED = "completed"
    ERROR = "error"
    STOPPED = "stopped"


class DownloadRetry(RetryController):
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


class DownloadTask(ABC):
    """Represents a download task with retry and flow control"""

    def __init__(self, url: str, save_path: Path):
        self._url = url
        self._save_path = save_path
        self._extension = ''
        self._total_size = 0
        self._stopped = False
        self._status = DownloadStatus.QUEUED

        # Composition
        self._retry = DownloadRetry()
        self._controller = FlowController()
        self._lock = threading.Lock()

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
        """Main execution loop"""
        with self._lock:
            while not self.completed and not self._stopped:
                try:
                    self._status = DownloadStatus.CONNECTING
                    self._perform_download_cycle()
                except Exception as e:
                    if self._stopped: break
                    self._status = DownloadStatus.ERROR
                    self._retry.make_retry(e)

            if self.completed:
                self._status = DownloadStatus.COMPLETED

    def _perform_download_cycle(self):
        """Handles a single attempt to connect and download"""
        # Headers
        downloaded = self.downloaded_size
        headers = {'Range': f'bytes={downloaded}-'} if downloaded > 0 else {}
        mode = 'ab' if downloaded > 0 else 'wb'

        # Make Request
        if self._stopped: return
        with requests.get(
                self.url, headers=headers, stream=True,
                timeout=(CONFIG.download_timeout_connect, CONFIG.download_timeout_request)
        ) as response:
            response.raise_for_status()
            self._check_response(response)

            self._update_metadata(response, downloaded)

            self._write_content(response, mode)

    def _update_metadata(self, response: Response, downloaded: int):
        """Extract extension and total size from response"""
        if not self._extension:
            self._extension = get_file_extension(response)

        content_length = int(response.headers.get('content-length', 0))

        # 206 means Partial Content (Resuming supported)
        if response.status_code == 206:
            self._total_size = downloaded + content_length
        else:
            self._total_size = content_length
            # Note: If server doesn't support 206, we might rewrite file,
            # handled by 'wb' mode logic in caller if needed,
            # but usually logic implies if not 206, we start over. (nah mostly)

    def _write_content(self, response: Response, mode: str):
        """Stream data to file with pause/stop handling"""
        with open(self.save_file, mode) as file:
            self._status = DownloadStatus.DOWNLOADING

            for chunk in response.iter_content(chunk_size=8192):
                self._controller.wait_resume()
                if self._stopped: return

                if chunk:
                    file.write(chunk)

    @property
    def url(self) -> str:
        return self._url

    @property
    def status(self) -> DownloadStatus:
        is_paused = self._status in (DownloadStatus.CONNECTING, DownloadStatus.DOWNLOADING) and self._controller.flag
        return DownloadStatus.PAUSED if is_paused else self._status

    @property
    def total_size(self) -> int:
        return self._total_size

    @property
    def controller(self):
        return self._controller

    @property
    def save_file(self) -> Path:
        return self._save_path.with_suffix(f".{self._extension}") if self._extension else self._save_path

    @property
    def downloaded_size(self) -> int:
        try:
            return self.save_file.stat().st_size
        except FileNotFoundError:
            return 0

    @property
    def completed(self) -> bool:
        return 0 < self._total_size <= self.downloaded_size

    @property
    def progress(self) -> float:
        return (self.downloaded_size / self.total_size * 100) if self.total_size > 0 else 0


class ImageDownloadTask(DownloadTask):
    """
    Download task for image files
    """

    def __init__(self, url: str, save_path: Path):
        corrected = correct_image_url(url)
        super().__init__(corrected, save_path)

    @staticmethod
    def _check_response(response: Response) -> None:
        if (ext := get_file_extension(response)) not in IMAGE_EXTENSIONS:
            raise FileExtensionNotSupported(ext)
