from pystache import render as render_text
from classes.models import ProgressLogLevel
from src.core.download_image import download_image
from src.core.process_image import process_image
from globals import user_input
from src.ui.progress import log_progress

#? Thay thế Text
def replace_text(slide, student: dict) -> bool:
    """
    Thay thế văn bản trong slide.

    Args:
        slide: Slide cần thay thế văn bản.
        student (dict): Thông tin sinh viên.

    Returns:
        bool: True nếu thay thế thành công, False nếu thất bại.
    """
    # Ghi log
    log_progress(__name__, ProgressLogLevel.INFO, "replace_text")
    
    # Chuẩn bị placeholders cho pystache
    placeholders = dict()
    for key in user_input.csv.placeholders:
        value = student.get(key)
        # Nếu giá trị là None hoặc không tồn tại, sử dụng chuỗi rỗng
        placeholders[key] = '' if value is None else value

    # Xử lý cho tất cả các shape có text trong slide
    for shape in slide.Shapes:
        if shape.HasTextFrame and shape.TextFrame.HasText:
            # Lấy văn bản gốc
            original_text = shape.TextFrame.TextRange.Text
            
            # Thay thế văn bản sử dụng pystache
            new_text = render_text(original_text, placeholders)
            
            # Chỉ cập nhật nếu có sự thay đổi
            if original_text != new_text:
                shape.TextFrame.TextRange.Text = new_text
    
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

def _replace_image_in_one_shape(slide, student_index: int, shape_index: int, image_url: str) -> bool:
    """
    Thay thế hình ảnh trong một shape.

    Args:
        slide: Slide cần thay thế hình ảnh.
        student_index (int): Chỉ số của sinh viên.
        shape_index (int): Chỉ số của shape.
        image_url (str): URL của hình ảnh.

    Returns:
        bool: True nếu thay thế thành công, False nếu thất bại.
    """
    # Nếu không có URL hình ảnh, giữ nguyên hình ảnh gốc
    if not image_url:
        log_progress(__name__, ProgressLogLevel.INFO, "keep_original_image", num=shape_index)
        return False
    
    # Tải hình ảnh
    image_path = download_image(image_url, student_index)
    if not image_path:
        log_progress(__name__, ProgressLogLevel.INFO, "keep_original_image", num=shape_index)
        return False
    
    # Xử lý hình ảnh
    shape = slide.Shapes(shape_index)
    processed_image_path = process_image(image_path, shape)
    
    # Thay thế hình ảnh
    _fill_image_into_filler(shape.Fill, processed_image_path, as_texture=False)
    
    # Ghi log
    log_progress(__name__, ProgressLogLevel.INFO, "replace_image", num=shape_index)
    return True

def replace_image(slide, student: dict, student_index: int) -> bool:
    """
    Thay thế hình ảnh trong slide.

    Args:
        slide: Slide cần thay thế hình ảnh.
        student (dict): Thông tin sinh viên.
        student_index (int): Chỉ số của sinh viên.

    Returns:
        bool: True nếu thay thế thành công, False nếu thất bại.
    """
    # Thay thế từng hình ảnh
    for config_image in user_input.config.images:
        shape_index = config_image.shape_index
        placeholder = config_image.placeholder
        
        # Nếu placeholder có trong thông tin sinh viên
        if placeholder in student:
            image_url = student[placeholder]
            _replace_image_in_one_shape(
                slide=slide,
                student_index=student_index,
                shape_index=shape_index,
                image_url=image_url
            )
    
    return True