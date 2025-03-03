from PyQt5.QtWidgets import QLineEdit
from src.logging.info import default as info
from src.logging.error import default as error

def no_slide(pptx_path: QLineEdit):
    """
    Ghi log khi không có slide nào trong file PowerPoint.

    Args:
        pptx_path (QLineEdit): Widget chứa đường dẫn file pptx.
    """
    pptx_path.clear()  # Xóa đường dẫn file pptx
    info(__name__, "pptx.no_slide")

def too_much_slide(pptx_path: QLineEdit):
    """
    Ghi log khi có quá nhiều slide trong file PowerPoint.

    Args:
        pptx_path (QLineEdit): Widget chứa đường dẫn file pptx.
    """
    pptx_path.clear()  # Xóa đường dẫn file pptx
    info(__name__, "pptx.too_much_slides")

def can_not_open(pptx_path: QLineEdit, e: Exception):
    """
    Ghi log khi không thể mở file PowerPoint.

    Args:
        pptx_path (QLineEdit): Widget chứa đường dẫn file pptx.
        e (Exception): Ngoại lệ xảy ra khi mở file.
    """
    pptx_path.clear()  # Xóa đường dẫn file pptx
    error(__name__, "pptx.can_not_open", str(e))

def always_read_only(pptx_path: QLineEdit):
    """
    Ghi log khi file PowerPoint luôn ở chế độ chỉ đọc.

    Args:
        pptx_path (QLineEdit): Widget chứa đường dẫn file pptx.
    """
    pptx_path.clear()  # Xóa đường dẫn file pptx
    error(__name__, "pptx.always_read_only")