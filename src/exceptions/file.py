class FileExtensionNotSupported(ValueError):
    """Unsupported file extension."""

    def __init__(self, extension: str):
        super().__init__(f"File extension \"'{extension}'\" is not supported.")
