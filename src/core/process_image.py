import os
from PIL import Image, UnidentifiedImageError
from classes.models import ProgressLogLevel
from globals import PROCESSED_PATH
from translations import format_text
from src.ui.progress import log_progress

#* Đăng ký các định dạng ảnh bổ sung
# AVIF
import pillow_avif  # noqa: F401
# HEIF
from pillow_heif import register_heif_opener
register_heif_opener()

def process_image(image_path: str, shape) -> str:
    """
    Xử lý hình ảnh để phù hợp với kích thước của shape.

    Args:
        image_path (str): Đường dẫn đến hình ảnh cần xử lý.
        shape: Shape cần thay thế hình ảnh.

    Returns:
        str: Đường dẫn đến hình ảnh đã xử lý.
    """
    log_progress(__name__, ProgressLogLevel.INFO, "process_image.start", image_path)

    # Lấy tên file từ đường dẫn
    file_name = os.path.basename(image_path)
    
    # Đường dẫn đến file đã xử lý
    processed_path = os.path.join(PROCESSED_PATH, file_name)
    
    # Đảm bảo thư mục PROCESSED_PATH tồn tại
    os.makedirs(os.path.dirname(processed_path), exist_ok=True)
    
    try:
        # Lấy kích thước của shape
        shape_width = shape.Width
        shape_height = shape.Height
        
        # Mở hình ảnh
        img = Image.open(image_path)
        
        # Thay đổi kích thước hình ảnh
        img = img.resize((int(shape_width), int(shape_height)))
        
        # Lưu hình ảnh đã xử lý
        img.save(processed_path)
        
        log_progress(__name__, ProgressLogLevel.INFO, "process_image.success")
        return processed_path
    except UnidentifiedImageError as e:
        error_message = format_text("errors.image_processing.unidentified", error=str(e))
        log_progress(__name__, ProgressLogLevel.ERROR, "process_image.error", error_message)
        log_progress(__name__, ProgressLogLevel.INFO, "process_image.skip")
        return image_path
    except OSError as e:
        error_message = format_text("errors.image_processing.os_error", error=str(e))
        log_progress(__name__, ProgressLogLevel.ERROR, "process_image.error", error_message)
        log_progress(__name__, ProgressLogLevel.INFO, "process_image.skip")
        return image_path
    except Exception as e:
        error_message = format_text("errors.image_processing.general", error=str(e))
        log_progress(__name__, ProgressLogLevel.ERROR, "process_image.error", error_message)
        log_progress(__name__, ProgressLogLevel.INFO, "process_image.skip")
        return image_path
