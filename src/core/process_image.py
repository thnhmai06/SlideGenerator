from typing import Callable, Type
from src.utils.image import crop
from PIL import Image

def process_image(image_path: str, shape, add_log: Callable[[str, str, str, str], None], loglevel: Type):
    add_log(__name__, loglevel.INFO, "process_image", image_path)
    with Image.open(image_path) as image:
        # Xử lý
        processed_image = crop.crop_image_to_aspect_ratio(image, shape.Width, shape.Height)
        
        # Lưu lại
        processed_image_path = image_path.replace(".png", "_processed.png")
        processed_image.save(processed_image_path)

    return processed_image_path
