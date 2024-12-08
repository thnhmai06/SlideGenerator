import os
from typing import Callable, Type
from pystache import render
from src.core._download_image import download_image
from globals import user_input, DOWNLOAD_PATH

#? Thay thế Text
def replace_text(slide, student: dict, add_log: Callable[[str, str, str, str], None], loglevel: Type) -> bool:
    placeholders = {key: student[key] for key in user_input.config.text}
    for shape in slide.Shapes:
        if shape.HasTextFrame and shape.TextFrame.HasText: # Xác nhận nó là shape text có chứa text
            text = shape.TextFrame.TextRange.Text
            new_text = render(text, placeholders)
            if (text != new_text):
                add_log(__name__, loglevel.INFO, "replace_text", f"{text} -> {new_text}")
                shape.TextFrame.TextRange.Text = new_text
    return True

#? Thay thế Image
def __each_item_replace_image(slide, num: int, shape_id: int, placeholder: str, add_log: Callable[[str, str, str, str], None], loglevel: Type) -> bool:
    # Tạo folder nếu thư mục lưu không tồn tại
    if not os.path.exists(DOWNLOAD_PATH):
        os.makedirs(DOWNLOAD_PATH)
    
    # Lấy ảnh từ link
    image_path = download_image(placeholder, num, add_log, loglevel)
    if not image_path:
        add_log(__name__, loglevel.INFO, "keep_original_image", shape_id)
        return False

    # Refill
    shape = slide.Shapes(shape_id)
    shape.Fill.UserPicture(image_path)

    # Thông báo đã thay thế ảnh
    add_log(__name__, loglevel.INFO, "replace_image", f"{shape_id}")
    return True

def replace_image(slide, student: dict, num: int, add_log: Callable[[str, str, str, str], None], loglevel: Type) -> bool:    
    for config_image_item in user_input.config.image:
        shape_id = config_image_item.shape_id
        link = student[config_image_item.placeholder]
        __each_item_replace_image(slide, num, shape_id, link, add_log, loglevel)
        
