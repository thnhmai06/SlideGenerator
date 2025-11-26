"""Custom exceptions for the core module."""

class FileExtensionNotSupported(ValueError):
    """Unsupported file extension."""

    def __init__(self, extension: str):
        super().__init__(f"File extension \"'{extension}'\" is not supported.")


class IndexOutOfRange(IndexError):
    """Raised when an index is out of the valid range."""

    def __init__(self, index: int, range_: tuple[int, int] = None):
        super().__init__(f"Index {index} is out of range {range_}.")
