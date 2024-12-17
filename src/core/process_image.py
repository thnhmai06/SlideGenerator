from src.utils.image import crop
from PIL import Image

def process_image(image_path, shape, add_log, loglevel):
    add_log(__name__, loglevel.INFO, "process_image", image_path)
    with Image.open(image_path) as image:
        # Xử lý
        processed_image = crop.crop_image_to_aspect_ratio(image, shape.Width, shape.Height)
        
        # Lưu lại
        processed_image.save(image_path)
