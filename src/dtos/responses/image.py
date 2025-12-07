from src.dtos.requests.image import AutoCropImageRequest
from src.dtos.responses.response import ImageResponse


class AutoCropImageResponse(ImageResponse):
    def __init__(self, request: AutoCropImageRequest, error: Exception = None):
        super().__init__(request, error)