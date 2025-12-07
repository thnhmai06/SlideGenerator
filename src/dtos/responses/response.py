import traceback
from abc import ABC

from pydantic import BaseModel

from src.dtos.requests.request import Request, DownloadRequest, ImageRequest, SheetRequest


class Response(BaseModel, ABC):
    def __init__(self, request: Request, success: bool):
        super().__init__()
        self.request = request
        self.success = success


class DownloadResponse(Response, ABC):
    def __init__(self, request: DownloadRequest):
        super().__init__(request, success=True)


class ImageResponse(Response, ABC):
    def __init__(self, request: ImageRequest):
        super().__init__(request, success=True)


class SheetResponse(Response, ABC):
    def __init__(self, request: SheetRequest):
        super().__init__(request, success=True)


class ErrorResponse(Response):
    def __init__(self, request: Request, error: Exception):
        super().__init__(request, success=False)
        self.kind = type(error).__name__
        self.message = str(error)
        self.tb = "".join(traceback.format_exception(type(error), error, error.__traceback__))
