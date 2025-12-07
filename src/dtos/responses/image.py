from src.dtos.requests.image import CropImageRequest
from src.dtos.responses.response import ImageResponse


class CropImageResponse(ImageResponse):
    """Response DTO for cropping an image."""

    def __init__(self, request: CropImageRequest):
        super().__init__(request)
