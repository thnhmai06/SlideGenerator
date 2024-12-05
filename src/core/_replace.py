from typing import Callable, Type
from pystache import render
from src.core._download_image import download_image
from globals import user_input, DOWNLOAD_PATH

def replace_text(slide, student: dict, add_log: Callable[[str, str, str, str], None], loglevel: Type) -> bool:
    placeholders = {key: student[key] for key in user_input.config.text}
    for shape in slide.Shapes:
        if shape.HasTextFrame and shape.TextFrame.HasText: # Xác nhận nó là shape text có chứa text
            text = shape.TextFrame.TextRange.Text
            new_text = render(text, placeholders)
            if (text != new_text):
                add_log(__name__, loglevel.info, "replace_text", f"{text} -> {new_text}")
                shape.TextFrame.TextRange.Text = new_text
    return True

def replace_image(slide, student: dict, num: int, add_log: Callable[[str, str, str, str], None], loglevel: Type) -> bool:
    for config_image_item in user_input.config.image:
        image_path = download_image(student[config_image_item.placeholder], DOWNLOAD_PATH, add_log, loglevel)
        if not image_path:
            return False

        # Thay thế ảnh bằng cách tạo một shape mới và xóa shape cũ
        # Tại sao phải làm vậy? Vì không thể thay đổi source của một shape ảnh có sẵn: https://stackoverflow.com/q/10169011/16410937
        sample_shape = slide.Shapes(config_image_item.shape_id)
        new_shape = slide.Shapes.AddPicture(
            FileName=image_path, 
            LinkToFile=0, 
            SaveWithDocument=-1, 
            Left=sample_shape.Left, 
            Top=sample_shape.Top, 
            Width=sample_shape.Width, 
            Height=sample_shape.Height
        )
        new_shape.Name = sample_shape.Name + f"_{num}"

        # Sao chép Format của shape cũ sang shape mới
        sample_shape.PickUp()
        new_shape.Apply()

        # Sao chép Animation của shape cũ sang shape mới
        sample_shape.PickUpAnimation()
        new_shape.ApplyAnimation()

        # Khôi phục zOrder từ shape cũ sang shape mới
        new_shape.ZOrder(sample_shape.ZOrderPosition)

        # Khôi phục group của shape cũ sang shape mới
        new_shape.GroupItems(sample_shape.GroupItems)

        #TODO: Continue here
