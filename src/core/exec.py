from typing import TYPE_CHECKING
from globals import user_input, pptx
from src.core._slide_utils import duplicate_slide
from src.core._replace import replace_text_placeholders
import shutil

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress
import os

class loglevel:
    info = "info"
    error = "error"

def _copy_file(path: str, save_path: str):
    if (path == save_path):
        return    
    if os.path.exists(save_path):
        os.remove(save_path)
    shutil.copyfile(path, save_path)

def per_processing(progress: "Progress", num: int):
    progress.add_log(__name__, loglevel.info, "read_student", num)
    student = user_input.csv.get(num)[0]

    progress.add_log(__name__, loglevel.info, "duplicate_slide")
    slide = duplicate_slide(pptx.presentation)

    replace_text_placeholders(slide, student, progress.add_log, loglevel)

    #TODO: Download and Replace image here

    progress.add_log(__name__, loglevel.info, "saving")
    pptx.presentation.Save()

    progress.add_log(__name__, loglevel.info, "finish_replace")

def exec(progress: "Progress", from_: int, to_: int):
    #* Chuẩn bị
    # Sao chép file gốc sang file lưu
    progress.add_log(__name__, loglevel.info, "create_file")
    _copy_file(user_input.pptx.path, user_input.save.path)
    
    # Tạo giao thức với PowerPoint
    progress.add_log(__name__, loglevel.info, "open_instance")
    pptx.open_instance()

    # Mở file PowerPoint
    progress.add_log(__name__, loglevel.info, "open_presentation")
    pptx.open_presentation(user_input.save.path)

    #* Bắt đầu
    progress.add_log(__name__, loglevel.info, "start")
    for num in range(to_, from_ - 1, -1): # Xử lí lùi để cho các slide đúng theo thứ tự trong file csv 
        per_processing(progress, num)

    #* Kết thúc
    progress.add_log(__name__, loglevel.info, "done")
    progress.add_log(__name__, loglevel.info, "close_presentation")
    pptx.close_presentation()
    progress.add_log(__name__, loglevel.info, "save_path", user_input.save.path)