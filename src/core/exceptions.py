"""Custom exceptions for the core module."""
from pathlib import Path


class FileExtensionNotSupported(ValueError):
    """Unsupported file extension."""

    def __init__(self, extension: str):
        super().__init__(f"File extension \"'{extension}'\" is not supported.")


class IndexOutOfRange(IndexError):
    """Raised when an index is out of the valid range."""

    def __init__(self, index: int, range_: tuple[int, int] = None):
        super().__init__(f"Index {index} is out of range {range_}.")

class ComputeSaliencyFailed(Exception):
    """Raised when saliency computation fails."""

    def __init__(self, file_path: Path):
        super().__init__("Failed to compute saliency map for: " + str(file_path))

class ReadImageFailed(Exception):
    """Raised when reading an image fails."""

    def __init__(self, file_path: Path):
        super().__init__("Failed to read image from: " + str(file_path))