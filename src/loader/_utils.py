def reduce_image_quality(image_bytes: bytes, quality: int) -> bytes:
    """
    Reduce the quality of the image to the minimum visible level.
    
    Args:
    - image_bytes: The original image in bytes.
    - quality: The quality of the reduced image (0-100).
    
    Returns:
    - The reduced quality image in bytes.
    """
    import io
    from PIL import Image

    # Open the image from bytes
    image = Image.open(io.BytesIO(image_bytes))
    
    # Create a BytesIO object to save the reduced quality image
    output = io.BytesIO()
    
    # Save the image with the lowest quality
    image.save(output, format = image.format, quality = quality)
    
    # Get the reduced quality image bytes
    reduced_image_bytes = output.getvalue()
    
    return reduced_image_bytes

def delete_all_file(PATH: str):
    import os
    if os.path.exists(PATH):
        for filename in os.listdir(PATH):
            file_path = os.path.join(PATH, filename)
            if os.path.isfile(file_path) or os.path.islink(file_path):
                os.unlink(file_path)
            elif os.path.isdir(file_path):
                os.rmdir(file_path)