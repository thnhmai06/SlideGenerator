"""Validation utility functions"""

import os


def validate_image_content(content_type: str, filename: str) -> bool:
    """
    Validate if content is an image
    
    Args:
        content_type: Content-Type header value
        filename: Filename with extension
        
    Returns:
        bool: True if valid image, False otherwise
    """
    # Check content type
    if content_type.startswith("image/"):
        return True
    
    # Check file extension
    ext = os.path.splitext(filename)[1].lower()
    valid_extensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg"]
    
    return ext in valid_extensions
