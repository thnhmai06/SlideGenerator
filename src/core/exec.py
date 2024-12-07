import threading
from typing import TYPE_CHECKING
from globals import user_input, DOWNLOAD_PATH
from classes.models import PowerPoint
from src.core._replace import replace_text, replace_image
from src.utils.file import copy_file, delete_file

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

SAMPLE_SLIDE_INDEX = 1

def _each_student_execute(pptx: PowerPoint, progress: "Progress", num: int):
    # Lấy thông tin sinh viên thứ num
    progress.log.append(__name__, progress.log.LogLevels.INFO, "read_student", num)
    student = user_input.csv.get(num)[0]

    # Nhân bản slide
    progress.log.append(__name__, progress.log.LogLevels.INFO, "duplicate_slide")
    slide = pptx.presentation.Slides(SAMPLE_SLIDE_INDEX).Duplicate()

    # Thay thế Text
    if user_input.config.text:
        replace_text(slide, student, progress.log.append, progress.log.LogLevels)

    # Thay thế Image
    if user_input.config.image:
        replace_image(slide, student, num, progress.log.append, progress.log.LogLevels)

    # Lưu file
    progress.log.append(__name__, progress.log.LogLevels.INFO, "saving")
    pptx.presentation.Save()

    # Thông báo thay thế sinh viên này thành công
    progress.log.append(__name__, progress.log.LogLevels.INFO, "finish_replace")


def __execute(pptx: PowerPoint, progress: "Progress", from_: int, to_: int):
    # * Chuẩn bị
        # Xóa folder Download cũ
        delete_file(DOWNLOAD_PATH)

        # Sao chép file gốc sang file lưu
        progress.log.append(__name__, progress.log.LogLevels.INFO, "create_file")
        copy_file(user_input.pptx.path, user_input.save.path)

        # Tạo giao thức với PowerPoint
        progress.log.append(__name__, progress.log.LogLevels.INFO, "open_instance")
        pptx.open_instance()

        # Mở file PowerPoint
        progress.log.append(__name__, progress.log.LogLevels.INFO, "open_presentation")
        pptx.open_presentation(user_input.save.path)

    # * Bắt đầu
        progress.log.append(__name__, progress.log.LogLevels.INFO, "start")
        # For lùi để cho các slide khi tạo sẽ đúng theo thứ tự trong file csv
        for num in range(to_, from_ - 1, -1):
            _each_student_execute(pptx, progress, num)

    # * Kết thúc
        # Xóa slide đầu tiên (là slide mẫu)
        progress.log.append(__name__, progress.log.LogLevels.INFO, "delete_sample_slide")
        pptx.presentation.Slides(SAMPLE_SLIDE_INDEX).Delete()
        pptx.presentation.Save()

        # Đóng file PowerPoint
        progress.log.append(__name__, progress.log.LogLevels.INFO, "close_presentation")
        pptx.close_presentation()

        # Đóng giao thức với PowerPoint
        progress.log.append(__name__, progress.log.LogLevels.INFO, "close_instance")
        pptx.close_instance()

        # Thông báo hoàn thành
        progress.log.append(__name__, progress.log.LogLevels.INFO, "done")

        # Thông báo vị trí lưu file
        progress.log.append(__name__, progress.log.LogLevels.INFO, "save_path", user_input.save.path)

        return
    
def work(progress: "Progress", from_: int, to_: int):
    pptx = PowerPoint()
    worker = threading.Thread(name = "core", target=__execute, args=(pptx, progress, from_, to_))
    worker.start()

