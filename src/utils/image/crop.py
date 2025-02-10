from PIL import Image

def crop_image_to_aspect_ratio(image: Image.Image, target_width: int, target_height: int) -> Image.Image:
    """
    Cắt ảnh sao cho kích thước là lớn nhất, tỉ lệ với kích thước cho trước.

    Args:
        image (Image.Image): Ảnh cần cắt.
        target_width (int): Chiều rộng mục tiêu.
        target_height (int): Chiều cao mục tiêu.

    Returns:
        Image.Image: Ảnh đã được cắt.
    """
    img_width, img_height = image.size
    target_ratio = target_width / target_height
    img_ratio = img_width / img_height

    if img_ratio > target_ratio:
        new_width = int(target_ratio * img_height)
        offset = (img_width - new_width) // 2
        box = (offset, 0, img_width - offset, img_height)
    else:
        new_height = int(img_width / target_ratio)
        offset = (img_height - new_height) // 2
        box = (0, offset, img_width, img_height - offset)

    cropped_img = image.crop(box)
    return cropped_img
