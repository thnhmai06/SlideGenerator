from typing import Callable
from PIL import Image, ImageOps
from pillow_heif import register_heif_opener
from src.utils.image import crop
from classes.models import ProgressLogLevel

#* Đăng ký các định dạng ảnh bổ sung
# AVIF
import pillow_avif  # noqa: F401
# HEIF
register_heif_opener()

def process_image(image_path: str, shape, add_log: Callable[[str, str, str, str], None]) -> str:
    """
    Xử lý hình ảnh: xoay ảnh theo Exif, cắt ảnh theo tỉ lệ và lưu lại dưới dạng PNG.

    Args:
        image_path (str): Đường dẫn tới ảnh gốc.
        shape: Đối tượng shape chứa thông tin về kích thước.
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log.

    Returns:
        str: Đường dẫn tới ảnh đã được xử lý.
    """
    add_log(__name__, ProgressLogLevel.INFO, "process_image", image_path)
    try:
        with Image.open(image_path, 'r') as image:
            # Xoay ảnh theo Exif
            image = ImageOps.exif_transpose(image)
            # Cắt ảnh theo tỉ lệ kích thước của shape
            image = crop.crop_image_to_aspect_ratio(image, shape.Width, shape.Height)

            # Lưu lại ảnh dưới dạng PNG
            file_name, _ = image_path.rsplit('.', 1)
            processed_image_path = f"{file_name}_processed.png"
            image.save(processed_image_path)

            # Thông báo thành công
            add_log(__name__, ProgressLogLevel.INFO, "process_image_success")
            return processed_image_path
    except Exception as e:
        # Ghi log lỗi nếu có
        add_log(__name__, ProgressLogLevel.ERROR, "process_image_error", str(e))
        add_log(__name__, ProgressLogLevel.INFO, "skip_process_image")
        return image_path
