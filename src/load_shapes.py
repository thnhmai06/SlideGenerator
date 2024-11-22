import os
from typing import TYPE_CHECKING
from globals import SHAPES_PATH, Input
from pptx.shapes.picture import Picture
from pptx.presentation import Presentation
from pptx import Presentation as init_presentation
from logger.info import console_info, default as info
from src.toggle_config import toggle_config_image

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui

def __refresh_placeholders():
    # Làm mới placeholders ở local file này
    global __placeholders
    __placeholders = Input.csv.placeholders
def __save_shapes(prs: Presentation, slide_index = 0, save_path: str = SHAPES_PATH): # Slide đầu tiên có index = 0
    # Author: @oceantran27
    # Edit: @thnhmai06
    # Description: Hàm này sẽ lưu lại các Shapes ảnh (đã xác định trong shape_indices) vào thư mục SHAPES_PATH
    # Edit note: Đã gộp hàm get_image_shape_indices và save_images_from_shapes thành hàm này
    
    IMAGE_TYPE = 13 #ID của shape ảnh trong PowerPoint

    # Tạo folder nếu thư mục lưu không tồn tại
    if not os.path.exists(save_path):
        os.makedirs(save_path)
    # Xóa hết các file trong save_path
    for filename in os.listdir(save_path):
        file_path = os.path.join(save_path, filename)
        if os.path.isfile(file_path):
            os.remove(file_path)

    slide = prs.slides[slide_index]
    for __shape_index_win32COM in range(1, len(slide.shapes) + 1): 
        #__shape_index_win32COM là chỉ số của shape trong slide (theo Win32COM, vì win32COM đếm từ 1)
        # Phần range cộng thêm 1 vì range(a,b) chỉ lấy từ a -> b-1
        
        __shape_index_python_pptx = __shape_index_win32COM - 1 
        # Chỉ số của shape trong slide (theo python-pptx, vì python-pptx đếm từ 0)

        shape = slide.shapes[__shape_index_python_pptx]
        if shape.shape_type == IMAGE_TYPE:
            # Xác nhận rằng shape có kiểu Picture. Comment: Code cháy wá 🔥🔥🔥
            assert isinstance(shape, Picture)

            # Lấy dữ liệu ảnh từ shape
            image = shape.image
            image_bytes = image.blob

            # Lưu ảnh vào thư mục save_path
            image_path = os.path.join(save_path, f"{__shape_index_python_pptx + 1}.{image.ext}")
            with open(image_path, "wb") as img_file:
                img_file.write(image_bytes)
                # Lưu thông tin ảnh vào Input.shapes
                Input.shapes.add(__shape_index_python_pptx, image_path)
            console_info(__name__, f"Image ID: {__shape_index_win32COM} -> {image_path} (Preview)")


def load(ui: 'Ui'):
    pptx_path = ui.pptx_path.text()
    prs = init_presentation(pptx_path)

    toggle_config_image(ui, False)
    ui.config_image_table.clearContents()

    # Nếu prs không có slide nào
    if not prs.slides:
        ui.pptx_path.clear() # Xóa đường dẫn file pptx
        info(__name__, "no_slide_pptx")
        return
    
    __save_shapes(prs) # Lưu các ảnh từ slide đầu tiên vào thư mục SHAPES_PATH

    __refresh_placeholders()
    # Chỉ khi đã có sẵn placeholder rồi thì mới enable config_image_table
    if (len(__placeholders) > 0):
        toggle_config_image(ui, True)
        