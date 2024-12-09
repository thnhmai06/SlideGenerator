from typing import TYPE_CHECKING
from globals import user_input, DOWNLOAD_PATH
from PyQt5.QtCore import QObject, pyqtSignal
from classes.models import PowerPoint
from src.core.replace import replace_text, replace_image
from src.utils.file import copy_file, delete_file

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

SAMPLE_SLIDE_INDEX = 1

class CoreWorker(QObject):
    progress_bar_setValue = pyqtSignal(int)
    onFinished = pyqtSignal()
    
    def __init__(self, pptx: PowerPoint, progress: "Progress", from_: int, to_: int):
        super().__init__()
        # Liệu có xảy ra Đệ quy không?
        # Không, vì đây là truyền tham chiếu
        self.pptx = pptx
        self.progress = progress
        self.from_ = from_
        self.to_ = to_

    def _each(self, pptx: PowerPoint, progress: "Progress", num: int, count: int):
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

        # Thông báo thay thế sinh viên này thành công, cập nhật thanh progress_bar
        progress.log.append(__name__, progress.log.LogLevels.INFO, "finish_replace")
        self.progress_bar_setValue.emit(count)

    def run(self):
    # * Chuẩn bị
        # Xóa folder Download cũ
        delete_file(DOWNLOAD_PATH)

        # Sao chép file gốc sang file lưu
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "create_file")
        copy_file(user_input.pptx.path, user_input.save.path)

        # Tạo giao thức với PowerPoint
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "open_instance")
        self.pptx.open_instance()

        # Mở file PowerPoint
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "open_presentation")
        self.pptx.open_presentation(user_input.save.path)

    # * Bắt đầu
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "start")
        # For lùi để cho các slide khi tạo sẽ đúng theo thứ tự trong file csv
        for count, num in enumerate(range(self.to_, self.from_ - 1, -1), start=1):
            self._each(self.pptx, self.progress, num, count)

    # * Kết thúc
        # Xóa slide đầu tiên (là slide mẫu)
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "delete_sample_slide")
        self.pptx.presentation.Slides(SAMPLE_SLIDE_INDEX).Delete()
        self.pptx.presentation.Save()

        # Đóng file PowerPoint
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "close_presentation")
        self.pptx.close_presentation()

        # Đóng giao thức với PowerPoint
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "close_instance")
        self.pptx.close_instance()

        # Thông báo hoàn thành
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "done")

        # Thông báo vị trí lưu file
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "save_path", user_input.save.path)
        return self.onFinished.emit()
    

def work(progress: "Progress", from_: int, to_: int):
    progress.progress_bar.setMinimum(0)
    progress.progress_bar.setMaximum(to_ - from_ + 1)

    thread = progress.core_thread
    worker = CoreWorker(thread.powerpoint, progress, from_, to_)
    worker.moveToThread(thread)
    
    #* Tại sao ở đây lại không connect với thread.started signal mà lại gán trực tiếp thread.run?
    # Vì khi thread.start() được gọi, thread.started phải chờ một thời gian nhất định mới được emit (unoffical, 
    # nhưng thực nghiệm chứng minh là vậy) 
    # Nên là, gán trực tiếp luôn cho thread.run cho đỡ lằng nhằng
    thread.run = worker.run 
    worker.progress_bar_setValue.connect(progress.progress_bar.setValue)    
    worker.onFinished.connect(thread.quit)

    thread.start()