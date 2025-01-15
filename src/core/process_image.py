from typing import Callable
from classes.models import ProgressLogLevel
from src.utils.image import crop
from PIL import Image, ImageOps

def process_image(image_path: str, shape, add_log: Callable[[str, str, str, str], None]):
    add_log(__name__, ProgressLogLevel.INFO, "process_image", image_path)
    with Image.open(image_path, 'r') as image:
        # Xử lý
        image = ImageOps.exif_transpose(image) # Xoay ảnh theo Exif
        image = crop.crop_image_to_aspect_ratio(image, shape.Width, shape.Height)
        
        # Lưu lại
        file_name, file_extension = image_path.rsplit('.', 1)
        image_path = f"{file_name}_processed.{file_extension}"
        image.save(image_path)

    return image_path
