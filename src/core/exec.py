import threading
from typing import TYPE_CHECKING
from globals import user_input
from classes.models import PowerPoint
from src.core._slide_utils import duplicate_slide, delete_slide
from src.core._replace import replace_text, replace_image

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress


class ProgressLoglevel:
    info = "info"
    error = "error"


def __copy_file(path: str, save_path: str):
    import os
    import shutil

    if path == save_path:
        return
    if os.path.exists(save_path):
        os.remove(save_path)
    shutil.copyfile(path, save_path)


def __per_processing(pptx: PowerPoint, progress: "Progress", num: int):
    # Lấy thông tin sinh viên thứ num
    progress.log.append(__name__, ProgressLoglevel.info, "read_student", num)
    student = user_input.csv.get(num)[0]

    # Nhân bản slide
    progress.log.append(__name__, ProgressLoglevel.info, "duplicate_slide")
    slide = duplicate_slide(pptx.presentation, 1)

    # Thay thế Text
    if user_input.config.text:
        replace_text(slide, student, progress.log.append, ProgressLoglevel)

    # Thay thế Image
    if user_input.config.image:
        replace_image(slide, student, num, progress.log.append, ProgressLoglevel)

    # Lưu file
    progress.log.append(__name__, ProgressLoglevel.info, "saving")
    pptx.presentation.Save()

    # Thông báo thay thế sinh viên này thành công
    progress.log.append(__name__, ProgressLoglevel.info, "finish_replace")


def __execute(pptx: PowerPoint, progress: "Progress", from_: int, to_: int):
    # * Chuẩn bị
    # Sao chép file gốc sang file lưu
    progress.log.append(__name__, ProgressLoglevel.info, "create_file")
    __copy_file(user_input.pptx.path, user_input.save.path)

    # Tạo giao thức với PowerPoint
    progress.log.append(__name__, ProgressLoglevel.info, "open_instance")
    pptx.open_instance()

    # Mở file PowerPoint
    progress.log.append(__name__, ProgressLoglevel.info, "open_presentation")
    pptx.open_presentation(user_input.save.path)

    # * Bắt đầu
    progress.log.append(__name__, ProgressLoglevel.info, "start")
    # For lùi để cho các slide khi tạo sẽ đúng theo thứ tự trong file csv
    for num in range(to_, from_ - 1, -1):
        __per_processing(pptx, progress, num)

    # * Kết thúc
    # Xóa slide đầu tiên (là slide mẫu)
    progress.log.append(__name__, ProgressLoglevel.info, "delete_sample_slide")
    delete_slide(pptx.presentation, 1)

    # Đóng file PowerPoint
    progress.log.append(__name__, ProgressLoglevel.info, "close_presentation")
    pptx.close_presentation()

    # Đóng giao thức với PowerPoint
    progress.log.append(__name__, ProgressLoglevel.info, "close_instance")
    pptx.close_instance()

    # Thông báo hoàn thành
    progress.log.append(__name__, ProgressLoglevel.info, "done")

    # Thông báo vị trí lưu file
    progress.log.append(__name__, ProgressLoglevel.info, "save_path", user_input.save.path)

    #TODO: Cần return trong hàm excute để kết thúc công việc cho thread
    
def work(progress: "Progress", from_: int, to_: int):
    pptx = PowerPoint()
    worker = threading.Thread(name = "core", target=__execute, args=(pptx, progress, from_, to_))
    worker.start()

