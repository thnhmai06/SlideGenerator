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
def __copy_animation(slide, source_shape, target_shape):
    source_shape.PickUpAnimation()
    target_shape.ApplyAnimation()

    # 1. Sao chép Animation Order
     #! Hàm này sẽ làm hỏng các animation của slide thành Instantly

def __each_item_replace_image(slide, num: int, sample_shape_id: int, placeholder: str, add_log: Callable[[str, str, str, str], None], loglevel: Type) -> bool:
    # Tạo folder nếu thư mục lưu không tồn tại
    if not os.path.exists(DOWNLOAD_PATH):
        os.makedirs(DOWNLOAD_PATH)
    
    # Lấy ảnh từ link
    image_path = download_image(placeholder, num, add_log, loglevel)
    if not image_path:
        add_log(__name__, loglevel.INFO, "keep_original_image", sample_shape_id)
        return False

    # Thay thế ảnh bằng cách tạo một shape mới và xóa shape cũ
    # Tại sao phải làm vậy? Vì không thể thay đổi source của một shape ảnh có sẵn: 
    # 1. https://stackoverflow.com/q/10169011/16410937
    # 2. https://answers.microsoft.com/en-us/msoffice/forum/all/vba-powerpoint-picture-format-change-picture/ea4707de-4748-4226-bb36-09d7d7f508f1
    sample_shape = slide.Shapes(sample_shape_id)
    new_shape = slide.Shapes.AddPicture(
        FileName=image_path, 
        LinkToFile=0, 
        SaveWithDocument=-1, 
        Left=sample_shape.Left, 
        Top=sample_shape.Top, 
        Width=sample_shape.Width, 
        Height=sample_shape.Height
    )

    # Khôi phục tên của shape cũ
    new_shape.Name = sample_shape.Name + f"_{num}"
    # Sao chép Format của shape cũ sang shape mới
    sample_shape.PickUp()
    new_shape.Apply()
    # Sao chép Animation của shape cũ sang shape mới
    if sample_shape.AnimationSettings.Animate:
        __copy_animation(slide, sample_shape, new_shape)
    
    # Khôi phục zOrder từ shape cũ sang shape mới
    new_shape.ZOrder(sample_shape.ZOrderPosition)

    # Xóa shape cũ
    sample_shape.Delete()
    # Thông báo
    add_log(__name__, loglevel.INFO, "replace_image", f"{sample_shape_id}")
    return True

def replace_image(slide, student: dict, num: int, add_log: Callable[[str, str, str, str], None], loglevel: Type) -> bool:    
    for config_image_item in user_input.config.image:
        sample_shape_id = config_image_item.shape_id
        link = student[config_image_item.placeholder]
        __each_item_replace_image(slide, num, sample_shape_id, link, add_log, loglevel)
        
