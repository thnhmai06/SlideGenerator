"""Enhanced download manager with parallel chunks, retry logic, and multithread support"""

import os
import threading
import uuid
import time
import math
from typing import Dict, Optional, List, Tuple
from enum import Enum
from concurrent.futures import ThreadPoolExecutor, as_completed
import requests
from .get_image import get_image
from ..utils import (
    get_filename_from_response,
    validate_image_content,
    ensure_directory_exists,
    get_file_size,
    supports_resume,
    get_resume_header
)
from ..config import config


class DownloadStatus(Enum):
    QUEUED = "queued"
    PENDING = "pending"
    DETECTING = "detecting"
    DOWNLOADING = "downloading"
    PAUSED = "paused"
    COMPLETED = "completed"
    ERROR = "error"


class DownloadTask:
    def __init__(self, task_id: str, url: str, save_dir: str):
        self.task_id = task_id
        self.url = url
        self.save_dir = save_dir
        self.status = DownloadStatus.QUEUED
        self.progress = 0.0
        self.total_size = 0
        self.downloaded_size = 0
        self.file_path: Optional[str] = None
        self.error_message: Optional[str] = None
        self.supports_resume = False
        self.paused = False
        self.download_url: Optional[str] = None
        self.retry_count = 0
        self.last_retry_time: Optional[float] = None
        self.chunk_progress: Dict[int, int] = {}  # chunk_index -> downloaded_bytes
        self.lock = threading.Lock()


class RetryHandler:
    """Handle retry logic with exponential backoff"""
    
    @staticmethod
    def should_retry(task: DownloadTask, exception: Exception) -> bool:
        """Check if should retry based on exception and retry count"""
        if task.retry_count >= config.max_retries:
            return False
        
        # Check if exception is retryable
        if isinstance(exception, requests.RequestException):
            if hasattr(exception, 'response') and exception.response is not None:
                status_code = exception.response.status_code
                return status_code in config.retry_on_status_codes
            return True  # Network errors are retryable
        
        return False
    
    @staticmethod
    def get_retry_delay(task: DownloadTask) -> float:
        """Calculate retry delay with exponential backoff"""
        delay = config.initial_retry_delay * (
            config.retry_backoff_multiplier ** task.retry_count
        )
        return min(delay, config.max_retry_delay)
    
    @staticmethod
    def wait_before_retry(task: DownloadTask):
        """Wait with exponential backoff before retry"""
        delay = RetryHandler.get_retry_delay(task)
        task.last_retry_time = time.time()
        time.sleep(delay)


class ChunkDownloader:
    """Handle parallel chunk downloading"""
    
    @staticmethod
    def download_chunk(
        url: str,
        start: int,
        end: int,
        chunk_index: int,
        task: DownloadTask,
        temp_file_path: str
    ) -> Tuple[int, bool, Optional[str]]:
        """
        Download a single chunk
        
        Returns:
            Tuple of (chunk_index, success, error_message)
        """
        retry_count = 0
        
        while retry_count <= config.max_retries:
            try:
                headers = {"Range": f"bytes={start}-{end}"}
                response = requests.get(
                    url,
                    headers=headers,
                    stream=True,
                    timeout=(config.connect_timeout, config.request_timeout)
                )
                response.raise_for_status()
                
                # Write chunk to file
                chunk_file = f"{temp_file_path}.chunk{chunk_index}"
                downloaded = 0
                
                with open(chunk_file, 'wb') as f:
                    for data in response.iter_content(chunk_size=8192):
                        if task.paused:
                            return (chunk_index, False, "Paused")
                        
                        if data:
                            f.write(data)
                            downloaded += len(data)
                            
                            # Update task progress
                            with task.lock:
                                task.chunk_progress[chunk_index] = downloaded
                                task.downloaded_size = sum(task.chunk_progress.values())
                                if task.total_size > 0:
                                    task.progress = (task.downloaded_size / task.total_size) * 100
                
                return (chunk_index, True, None)
                
            except Exception as e:
                retry_count += 1
                if retry_count <= config.max_retries:
                    delay = config.initial_retry_delay * (
                        config.retry_backoff_multiplier ** (retry_count - 1)
                    )
                    delay = min(delay, config.max_retry_delay)
                    time.sleep(delay)
                else:
                    return (chunk_index, False, str(e))
        
        return (chunk_index, False, "Max retries exceeded")
    
    @staticmethod
    def merge_chunks(temp_file_path: str, num_chunks: int, final_path: str) -> bool:
        """Merge downloaded chunks into final file"""
        try:
            with open(final_path, 'wb') as outfile:
                for i in range(num_chunks):
                    chunk_file = f"{temp_file_path}.chunk{i}"
                    if not os.path.exists(chunk_file):
                        return False
                    
                    with open(chunk_file, 'rb') as infile:
                        outfile.write(infile.read())
                    
                    # Remove chunk file
                    os.remove(chunk_file)
            
            return True
        except Exception:
            return False


class DownloadManager:
    def __init__(self):
        self.tasks: Dict[str, DownloadTask] = {}
        self.lock = threading.Lock()
        self.download_queue: List[str] = []
        self.active_downloads = 0
        self.executor = ThreadPoolExecutor(max_workers=config.max_concurrent_downloads)
        self.queue_processor_thread = None
        self._start_queue_processor()
    
    def _start_queue_processor(self):
        """Start background thread to process download queue"""
        if self.queue_processor_thread is None or not self.queue_processor_thread.is_alive():
            self.queue_processor_thread = threading.Thread(target=self._process_queue, daemon=True)
            self.queue_processor_thread.start()
    
    def _process_queue(self):
        """Process download queue continuously"""
        while True:
            try:
                with self.lock:
                    if (self.download_queue and 
                        self.active_downloads < config.max_concurrent_downloads):
                        task_id = self.download_queue.pop(0)
                        task = self.tasks.get(task_id)
                        if task and task.status == DownloadStatus.QUEUED:
                            self.active_downloads += 1
                            self.executor.submit(self._download_with_cleanup, task_id)
                
                time.sleep(0.1)  # Small delay to prevent busy loop
            except Exception:
                time.sleep(1)
    
    def _download_with_cleanup(self, task_id: str):
        """Wrapper to handle download and cleanup"""
        try:
            self._download(task_id)
        finally:
            with self.lock:
                self.active_downloads -= 1

    def create_task(self, url: str, save_dir: Optional[str] = None) -> str:
        """Create new download task and add to queue"""
        task_id = str(uuid.uuid4())
        
        # Use configured download directory if not specified
        if save_dir is None:
            save_dir = config.download_dir
        
        # Create directory if not exists
        ensure_directory_exists(save_dir)
        
        task = DownloadTask(task_id, url, save_dir)
        
        with self.lock:
            self.tasks[task_id] = task
            self.download_queue.append(task_id)
        
        return task_id

    def _download(self, task_id: str):
        """Execute download with retry logic"""
        task = self.tasks[task_id]
        
        while True:
            try:
                # Step 1: Detect download link
                task.status = DownloadStatus.DETECTING
                download_url = get_image(task.url)
                task.download_url = download_url
                
                # Step 2: Get file info
                task.status = DownloadStatus.PENDING
                response = requests.head(
                    download_url,
                    timeout=(config.connect_timeout, config.request_timeout)
                )
                response.raise_for_status()
                
                # Get filename and paths
                filename = get_filename_from_response(response, download_url)
                file_path = os.path.join(task.save_dir, filename)
                temp_file_path = file_path + ".part"
                task.file_path = file_path
                
                # Check resume support and file size
                task.supports_resume = supports_resume(response)
                task.total_size = int(response.headers.get("content-length", 0))
                
                # Validate image content
                content_type = response.headers.get("content-type", "")
                if not validate_image_content(content_type, filename):
                    raise ValueError(
                        f"URL does not point to an image. Content-Type: {content_type}"
                    )
                
                # Step 3: Download
                task.status = DownloadStatus.DOWNLOADING
                
                # Decide download strategy
                use_parallel = (
                    config.enable_parallel_chunks and
                    task.supports_resume and
                    task.total_size >= config.min_file_size_for_parallel
                )
                
                if use_parallel:
                    self._download_parallel_chunks(task, download_url, temp_file_path, file_path)
                else:
                    self._download_sequential(task, download_url, temp_file_path, file_path)
                
                # Completed
                task.status = DownloadStatus.COMPLETED
                task.progress = 100.0
                return
                
            except Exception as e:
                if RetryHandler.should_retry(task, e):
                    task.retry_count += 1
                    RetryHandler.wait_before_retry(task)
                    continue
                else:
                    task.status = DownloadStatus.ERROR
                    task.error_message = f"{str(e)} (after {task.retry_count} retries)"
                    return
    
    def _download_sequential(
        self,
        task: DownloadTask,
        download_url: str,
        temp_file_path: str,
        file_path: str
    ):
        """Download file sequentially (traditional method)"""
        headers = {}
        
        # Check for partial download
        if task.supports_resume and os.path.exists(temp_file_path):
            task.downloaded_size = get_file_size(temp_file_path)
            headers = get_resume_header(task.downloaded_size)
        
        # Get file
        response = requests.get(
            download_url,
            stream=True,
            timeout=(config.connect_timeout, config.request_timeout),
            headers=headers
        )
        response.raise_for_status()
        
        # Update total size if resuming
        if "Content-Range" in response.headers:
            content_range = response.headers["Content-Range"]
            total_size_str = content_range.split("/")[-1]
            task.total_size = int(total_size_str)
        
        # Download and write file
        mode = "ab" if task.downloaded_size > 0 else "wb"
        with open(temp_file_path, mode) as f:
            for chunk in response.iter_content(chunk_size=config.chunk_size):
                # Check if paused
                if task.paused:
                    task.status = DownloadStatus.PAUSED
                    return
                
                if chunk:
                    f.write(chunk)
                    task.downloaded_size += len(chunk)
                    
                    # Update progress
                    if task.total_size > 0:
                        task.progress = (task.downloaded_size / task.total_size) * 100
        
        # Rename temp file to final file
        if os.path.exists(temp_file_path):
            if os.path.exists(file_path):
                os.remove(file_path)
            os.rename(temp_file_path, file_path)
    
    def _download_parallel_chunks(
        self,
        task: DownloadTask,
        download_url: str,
        temp_file_path: str,
        file_path: str
    ):
        """Download file using parallel chunks"""
        # Calculate chunk ranges
        num_workers = min(config.max_workers_per_download, task.total_size // config.chunk_size + 1)
        chunk_size = math.ceil(task.total_size / num_workers)
        
        chunks = []
        for i in range(num_workers):
            start = i * chunk_size
            end = min(start + chunk_size - 1, task.total_size - 1)
            chunks.append((i, start, end))
        
        # Initialize chunk progress
        task.chunk_progress = {i: 0 for i in range(num_workers)}
        
        # Download chunks in parallel
        with ThreadPoolExecutor(max_workers=num_workers) as executor:
            futures = {
                executor.submit(
                    ChunkDownloader.download_chunk,
                    download_url,
                    start,
                    end,
                    chunk_index,
                    task,
                    temp_file_path
                ): chunk_index
                for chunk_index, start, end in chunks
            }
            
            failed_chunks = []
            for future in as_completed(futures):
                chunk_index, success, error_msg = future.result()
                if not success:
                    if error_msg != "Paused":
                        failed_chunks.append((chunk_index, error_msg))
                    else:
                        # Task was paused
                        task.status = DownloadStatus.PAUSED
                        return
        
        # Check if all chunks downloaded successfully
        if failed_chunks:
            error_messages = [f"Chunk {idx}: {msg}" for idx, msg in failed_chunks]
            raise Exception(f"Failed to download chunks: {'; '.join(error_messages)}")
        
        # Merge chunks
        if not ChunkDownloader.merge_chunks(temp_file_path, num_workers, file_path):
            raise Exception("Failed to merge downloaded chunks")
        
        # Clean up any remaining chunk files
        for i in range(num_workers):
            chunk_file = f"{temp_file_path}.chunk{i}"
            if os.path.exists(chunk_file):
                try:
                    os.remove(chunk_file)
                except Exception:
                    pass

    def pause_task(self, task_id: str) -> bool:
        """Pause download task"""
        with self.lock:
            task = self.tasks.get(task_id)
            if not task:
                return False
            
            if task.status == DownloadStatus.DOWNLOADING:
                task.paused = True
                return True
            
            return False
    
    def resume_task(self, task_id: str) -> bool:
        """Resume paused download task"""
        with self.lock:
            task = self.tasks.get(task_id)
            if not task:
                return False
            
            if task.status == DownloadStatus.PAUSED:
                if not task.supports_resume:
                    task.error_message = "Server does not support resume"
                    task.status = DownloadStatus.ERROR
                    return False
                
                task.paused = False
                task.status = DownloadStatus.QUEUED
                self.download_queue.append(task_id)
                
                return True
            
            return False

    def get_status(self, task_id: str) -> Optional[Dict]:
        """Get task status"""
        with self.lock:
            task = self.tasks.get(task_id)
            if not task:
                return None
            
            return {
                "task_id": task.task_id,
                "url": task.url,
                "status": task.status.value,
                "progress": round(task.progress, 2),
                "total_size": task.total_size,
                "downloaded_size": task.downloaded_size,
                "file_path": task.file_path,
                "error_message": task.error_message,
                "is_downloaded": task.status == DownloadStatus.COMPLETED,
                "supports_resume": task.supports_resume,
                "retry_count": task.retry_count,
            }

    def get_all_tasks(self) -> List[Dict]:
        """Get list of all tasks"""
        with self.lock:
            return [
                status
                for task_id in self.tasks.keys()
                if (status := self.get_status(task_id)) is not None
            ]

    def cancel_task(self, task_id: str) -> bool:
        """Cancel task"""
        with self.lock:
            task = self.tasks.get(task_id)
            if not task:
                return False
            
            # Remove from queue if queued
            if task_id in self.download_queue:
                self.download_queue.remove(task_id)
            
            # Mark as paused to stop download
            task.paused = True
            
            # Remove from tasks
            del self.tasks[task_id]
            return True
    
    def get_queue_info(self) -> Dict:
        """Get information about download queue"""
        with self.lock:
            return {
                "queued_tasks": len(self.download_queue),
                "active_downloads": self.active_downloads,
                "max_concurrent": config.max_concurrent_downloads,
                "queue": self.download_queue.copy()
            }


# Singleton instance
download_manager = DownloadManager()
