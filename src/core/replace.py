import os
from typing import Callable
from pystache import render
from classes.models import ProgressLogLevel
from src.core.download_image import download_image
from src.core.process_image import process_image
from globals import user_input, DOWNLOAD_PATH

#? Thay thế Text
def replace_text(slide, student: dict, add_log: Callable[[str, str, str, str], None]) -> bool:
    placeholders = {key: student[key] for key in user_input.config.text}
    for shape in slide.Shapes:
        if shape.HasTextFrame and shape.TextFrame.HasText: # Xác nhận nó là shape text có chứa text
            text = shape.TextFrame.TextRange.Text
            new_text = render(text, placeholders)
            if (text != new_text):
                add_log(__name__, ProgressLogLevel.INFO, "replace_text", f"{text} -> {new_text}")
                shape.TextFrame.TextRange.Text = new_text
    return True

def __fill_picture(fill, image_path: str):
    # Lưu lại thiết lập của fill trước khi thay đổi
    transparency_before = fill.Transparency  # Độ trong suốt

    # Thay đổi fill
    fill.UserPicture(image_path)

    # Phục hồi các thiết lập cũ
    fill.Transparency = transparency_before  # Độ trong suốt

def __fill_texture(fill, image_path: str):
    # Lưu lại thiết lập của fill trước khi thay đổi
    alignment_before = fill.TextureAlignment  # Căn chỉnh
    tile_before = fill.TextureTile  # True nếu Tile ảnh
    transparency_before = fill.Transparency  # Độ trong suốt
    offset_x_before = fill.TextureOffsetX  # Vị trí X của texture
    offset_y_before = fill.TextureOffsetY  # Vị trí Y của texture
    scale_h_before = fill.TextureHorizontalScale  # Scale ngang
    scale_v_before = fill.TextureVerticalScale  # Scale dọc

    # Thay đổi fill
    fill.UserTextured(image_path)

    # Phục hồi các thiết lập cũ
    fill.TextureAlignment = alignment_before  # Căn chỉnh
    fill.TextureTile = tile_before  # Bật/tắt chế độ Tile
    fill.TextureOffsetX = offset_x_before  # Offset X
    fill.TextureOffsetY = offset_y_before  # Offset Y
    fill.TextureHorizontalScale = scale_h_before  # Scale ngang
    fill.TextureVerticalScale = scale_v_before  # Scale dọc
    fill.Transparency = transparency_before  # Độ trong suốt

#? Thay thế Image
def __each_item_replace_image(slide, num: int, shape_index: int, placeholder: str, add_log: Callable[[str, str, str, str], None]) -> bool:
    # Tạo folder nếu thư mục lưu không tồn tại
    if not os.path.exists(DOWNLOAD_PATH):
        os.makedirs(DOWNLOAD_PATH)
    
    # Lấy ảnh từ link
    image_path = download_image(placeholder, num, add_log)
    if not image_path:
        add_log(__name__, ProgressLogLevel.INFO, "keep_original_image", shape_index)
        return False
    
    shape = slide.Shapes(shape_index)
    
    # Xử lý hình ảnh
    processed_image_path = process_image(image_path, shape, add_log)

    # Refill
    # MSO_FILLPICTURE = 6  # https://learn.microsoft.com/en-us/office/vba/api/office.msofilltype#:~:text=Patterned%20fill-,msoFillPicture,-6
    # MSO_FILLTEXTURED = 4 # https://learn.microsoft.com/en-us/office/vba/api/office.msofilltype#:~:text=Solid%20fill-,msoFillTextured,-4
    __fill_picture(shape.Fill, processed_image_path)
    # if (shape.Fill.Type == MSO_FILLPICTURE):
    #     __fill_picture(shape.Fill, image_path)
    # elif (shape.Fill.Type == MSO_FILLTEXTURED):
    #     __fill_texture(shape.Fill, image_path)

    # Thông báo đã thay thế ảnh
    add_log(__name__, ProgressLogLevel.INFO, "replace_image", f"{shape_index}")
    return True

def replace_image(slide, student: dict, num: int, add_log: Callable[[str, str, str, str], None]) -> bool:    
    for config_image_item in user_input.config.image:
        shape_index = config_image_item.shape_index
        link = student[config_image_item.placeholder]
        __each_item_replace_image(slide, num, shape_index, link, add_log)
        
