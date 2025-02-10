import os
from typing import Callable
from pystache import render as render_text
from classes.models import ProgressLogLevel
from src.core.download_image import download_image
from src.core.process_image import process_image
from globals import user_input, DOWNLOAD_PATH

#? Thay thế Text
def replace_text(slide, student: dict, add_log: Callable[[str, str, str, str], None]) -> bool:
    """
    Thay thế văn bản trong slide bằng thông tin từ sinh viên.

    Args:
        slide: Slide PowerPoint cần thay thế văn bản.
        student (dict): Thông tin sinh viên.
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log.

    Returns:
        bool: Trả về True nếu thay thế thành công.
    """
    placeholders = {key: student[key] if key in student and student[key] else '' for key in user_input.config.text}
    for shape in slide.Shapes:
        if shape.HasTextFrame and shape.TextFrame.HasText:  # Xác nhận nó là shape text có chứa text
            original_text = shape.TextFrame.TextRange.Text
            new_text = render_text(original_text, placeholders)
            if original_text != new_text:
                add_log(__name__, ProgressLogLevel.INFO, "replace_text")
                add_log(__name__, ProgressLogLevel.INFO, "replace_text_original", original_text)
                shape.TextFrame.TextRange.Text = new_text
                add_log(__name__, ProgressLogLevel.INFO, "replace_text_new", new_text)
    return True

#? Thay thế Image
def _fill_image_into_filler(fill, image_path: str, as_texture: bool = False):
    """
    Điền hình ảnh vào filler của shape.

    Args:
        fill: Đối tượng fill của shape.
        image_path (str): Đường dẫn tới hình ảnh.
        as_texture (bool): Nếu True, điền hình ảnh dưới dạng texture.
    """
    if as_texture:
        # Lưu lại thiết lập cũ
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
    else:  # as_picture
        # Lưu lại thiết lập cũ
        transparency_before = fill.Transparency  # Độ trong suốt

        # Thay đổi fill
        fill.UserPicture(image_path)

        # Phục hồi các thiết lập cũ
        fill.Transparency = transparency_before  # Độ trong suốt

def _replace_image_in_one_shape(slide, student_index: int, shape_index: int, image_url: str, add_log: Callable[[str, str, str, str], None]) -> bool:
    """
    Thay thế hình ảnh trong một shape của slide.

    Args:
        slide: Slide PowerPoint cần thay thế hình ảnh.
        student_index (int): Chỉ số sinh viên.
        shape_index (int): Chỉ số shape trong slide.
        image_url (str): URL của hình ảnh.
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log.

    Returns:
        bool: Trả về True nếu thay thế thành công.
    """
    # Tạo folder nếu thư mục lưu không tồn tại
    if not os.path.exists(DOWNLOAD_PATH):
        os.makedirs(DOWNLOAD_PATH)
    
    # Lấy ảnh từ link
    image_path = download_image(image_url, student_index, add_log)
    if not image_path:
        add_log(__name__, ProgressLogLevel.INFO, "keep_original_image", shape_index)
        return False
    
    # Xử lý hình ảnh
    shape = slide.Shapes(shape_index)
    processed_image_path = process_image(image_path, shape, add_log)

    # Refill
    _fill_image_into_filler(shape.Fill, processed_image_path)

    # Thông báo đã thay thế ảnh
    add_log(__name__, ProgressLogLevel.INFO, "replace_image", f"{shape_index}")
    return True

def replace_image(slide, student: dict, student_index: int, add_log: Callable[[str, str, str, str], None]) -> bool:
    """
    Thay thế hình ảnh trong slide bằng thông tin từ sinh viên.

    Args:
        slide: Slide PowerPoint cần thay thế hình ảnh.
        student (dict): Thông tin sinh viên.
        student_index (int): Chỉ số sinh viên.
        add_log (Callable[[str, str, str, str], None]): Hàm ghi log.

    Returns:
        bool: Trả về True nếu thay thế thành công.
    """
    for config_image_item in user_input.config.image:
        shape_index = config_image_item.shape_index
        image_url = student[config_image_item.placeholder]
        _replace_image_in_one_shape(
            slide=slide, 
            student_index=student_index, 
            shape_index=shape_index, 
            image_url=image_url, 
            add_log=add_log
        )

