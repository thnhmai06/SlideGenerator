from abc import ABC
from enum import Enum

from pydantic import BaseModel


class RequestType(Enum):
    Download = "Download"
    Image = "Image"
    Sheet = "Sheet"


class Request(BaseModel, ABC):
    def __init__(self, request_type: RequestType):
        super().__init__()
        self.request_type = request_type


class DownloadRequest(Request, ABC):
    def __init__(self, url: str, save_path: str):
        super().__init__(RequestType.Download)
        self.url = url
        self.save_path = save_path


class ImageRequest(Request, ABC):
    def __init__(self, file_path: str):
        super().__init__(RequestType.Image)
        self.file_path = file_path


class SheetRequest(Request, ABC):
    def __init__(self, sheet_path: str):
        super().__init__(RequestType.Sheet)
        self.sheet_path = sheet_path
