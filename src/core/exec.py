from typing import TYPE_CHECKING
from globals import user_input, pptx
import shutil

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

class LogLevel():
    info = "info"
    error = "error"
__loglevel = LogLevel()

def _copy_file(path: str, save_path: str):
    if (path == save_path):
        return
    shutil.copyfile(path, save_path)

def _per_processing(progress: "Progress", num: int):
    progress.add_log(__name__, __loglevel.info, "read_student", num)
    student = user_input.csv.get(num)

    pass
    #TODO: Continue here

def exec(progress: "Progress", from_: int, to_: int):
    # Sao chép file gốc sang file lưu
    progress.add_log(__name__, __loglevel.info, "create_file")
    _copy_file(user_input.pptx.path, user_input.save.path)
    
    # Tạo giao thức với PowerPoint
    progress.add_log(__name__, __loglevel.info, "open_instance")
    pptx.open_instance()

    # Mở file PowerPoint
    progress.add_log(__name__, __loglevel.info, "open_presentation")
    pptx.open_presentation(user_input.pptx.path)

    # Bắt đầu
    progress.add_log(__name__, __loglevel.info, "start")
    for num in range(from_, to_ + 1): 
        _per_processing(progress, num)
