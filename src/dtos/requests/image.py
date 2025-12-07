from enum import Enum

from src.dtos.requests.request import ImageRequest

class ImageRequestType(Enum):
    Crop = "crop"

class CropImageRequest(ImageRequest):
    class CropMode(Enum):
        Prominent = "prominent"
        Center = "center"

    def __init__(self, file_path: str, width: int, height: int, mode: CropMode = CropMode.Prominent):
        super().__init__(file_path)
        self.mode = mode
        self.width = width
        self.height = height
