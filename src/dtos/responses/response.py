from abc import ABC

from src.dtos.requests.request import Request, DownloadRequest, ImageRequest, SheetRequest


class Response(ABC):
    def __init__(self, request: Request, error: Exception = None):
        self.request = request
        self.success = error is None
        if not self.success:
            self.kind = type(error).__name__
            self.message = str(error)


class DownloadResponse(Response, ABC):
    def __init__(self, request: DownloadRequest, error: Exception = None):
        super().__init__(request, error)


class ImageResponse(Response, ABC):
    def __init__(self, request: ImageRequest, error: Exception = None):
        super().__init__(request, error)


class SheetResponse(Response, ABC):
    def __init__(self, request: SheetRequest, error: Exception = None):
        super().__init__(request, error)
