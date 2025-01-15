from typing import Callable
from PIL import Image, ImageOps
from pillow_heif import register_heif_opener
from src.utils.image import crop
from classes.models import ProgressLogLevel

# Register Additions Image Formats
import pillow_avif  # noqa: F401
register_heif_opener()

def process_image(image_path: str, shape, add_log: Callable[[str, str, str, str], None]):
    add_log(__name__, ProgressLogLevel.INFO, "process_image", image_path)
    try:
        with Image.open(image_path, 'r') as image:
            # Xử lý
            image = ImageOps.exif_transpose(image) # Xoay ảnh theo Exif
            image = crop.crop_image_to_aspect_ratio(image, shape.Width, shape.Height)

            # Lưu lại
            file_name, file_extension = image_path.rsplit('.', 1)
            image_path = f"{file_name}_processed.png" # Buộc về dạng PNG, 
            # tránh trường hợp máy ko hỗ trợ các kiểu ảnh đặc biệt như HEIC, AVIF
            image.save(image_path)

            # Thông báo
            add_log(__name__, ProgressLogLevel.INFO, "process_image_success", image_path)
    except Exception as e:
        add_log(__name__, ProgressLogLevel.ERROR, "process_image_error", f"{e}")
        add_log(__name__, ProgressLogLevel.INFO, "skip_process_image")

    return image_path
