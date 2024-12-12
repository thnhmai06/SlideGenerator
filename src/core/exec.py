import traceback
from typing import TYPE_CHECKING
from PyQt5.QtCore import QObject, pyqtSignal
from classes.models import PowerPoint
from src.logging.error import show_err_diaglog
from src.core.replace import replace_text, replace_image
from src.utils.file import copy_file, delete_file
from globals import user_input, DOWNLOAD_PATH
from translations import TRANS

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

SAMPLE_SLIDE_INDEX = 1

class CoreWorker(QObject):
    progress_bar_setValue = pyqtSignal(int)
    progress_label_set_label = pyqtSignal(str, tuple)
    onFinished = pyqtSignal()
    show_err_diaglog = pyqtSignal(str, str, str)

    def __expection_handler(self, expection: Exception):
        expection_traceback = traceback.format_exc()
        if isinstance(expection, PermissionError):
            self.progress.log.append(
                __name__,
                self.progress.log.LogLevels.ERROR,
                "PermissionError",
                expection_traceback,
            )
            self.show_err_diaglog.emit(
                TRANS["diaglogs"]["error"]["window_name"],
                TRANS["progress"]["log"]["error"]["PermissionError"],
                expection_traceback,
            )
        else:
            self.progress.log.append(
                __name__,
                self.progress.log.LogLevels.ERROR,
                "uncaught_exception",
                expection_traceback,
            )
            self.show_err_diaglog.emit(
                f"{TRANS["progress"]["log"]["error"]["uncaught_exception"]}\n",
                f"{TRANS["progress"]["log"]["error"]["uncaught_exception"]}\n\n{str(expection)}",
                expection_traceback,
            )
        return self.onFinished.emit()

    def __init__(self, pptx: PowerPoint, progress: "Progress", from_: int, to_: int):
        super().__init__()
        # Liệu có xảy ra Đệ quy không?
        # Không, vì đây là truyền tham chiếu
        self.pptx = pptx
        self.progress = progress
        self.from_ = from_
        self.to_ = to_

    def _each(self, num: int, count: int, total: int):
        self.progress_label_set_label.emit("replacing", (str(num), f"({count}/{total})"))

        # Lấy thông tin sinh viên thứ num
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "read_student", num)
        student = user_input.csv.get(num)[0]

        # Nhân bản slide
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "duplicate_slide")
        slide = self.pptx.presentation.Slides(SAMPLE_SLIDE_INDEX).Duplicate()

        # Thay thế Text
        if user_input.config.text:
            replace_text(slide, student, self.progress.log.append, self.progress.log.LogLevels)

        # Thay thế Image
        if user_input.config.image:
            replace_image(slide, student, num, self.progress.log.append, self.progress.log.LogLevels)

        # Lưu file
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "saving")
        self.pptx.presentation.Save()

        # Thông báo thay thế sinh viên này thành công, cập nhật thanh progress_bar
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "finish_replace")
        self.progress_bar_setValue.emit(count)

    def run(self):
    # * Chuẩn bị
        self.progress_label_set_label.emit("perparing", ())
        # Xóa folder Download cũ (nếu có)
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
            self._each(num, count, self.to_ - self.from_ + 1)

        self.progress_label_set_label.emit("finishing", ())
    # * Kết thúc
        # Xóa slide đầu tiên (là slide mẫu)
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "delete_sample_slide")
        self.pptx.presentation.Slides(SAMPLE_SLIDE_INDEX).Delete()
        self.pptx.presentation.Save()

        # Xóa các ảnh đã tải xuống
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "delete_downloaded_image")
        delete_file(DOWNLOAD_PATH)

        # Đóng file PowerPoint
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "close_presentation")
        self.pptx.close_presentation()

        # Đóng giao thức với PowerPoint
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "close_instance")
        self.pptx.close_instance()

        # Thông báo hoàn thành
        self.progress_label_set_label.emit("finished", ())
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "done")

        # Thông báo vị trí lưu file
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "save_path", user_input.save.path)
        return self.onFinished.emit()
    
    def start(self):
        try:
            self.run()
        except Exception as e:
            self.__expection_handler(e)

def work(progress: "Progress", from_: int, to_: int):
    progress.progress_bar.setMinimum(0)
    progress.progress_bar.setMaximum(to_ - from_ + 1)

    thread = progress.core_thread
    worker = CoreWorker(thread.powerpoint, progress, from_, to_)
    # worker.moveToThread(thread)
    
    #* Tại sao ở đây lại không connect với thread.started signal mà lại gán trực tiếp thread.run?
    # Vì khi thread.start() được gọi, thread.started phải chờ một thời gian nhất định mới được emit (unoffical, 
    # nhưng thực nghiệm chứng minh là vậy) 
    # Nên là, gán trực tiếp luôn cho thread.run cho đỡ lằng nhằng
    thread.run = worker.start 
    worker.progress_bar_setValue.connect(progress.progress_bar.setValue)    
    worker.progress_label_set_label.connect(progress.label.set_label)
    worker.onFinished.connect(lambda: {
        progress.done_button_toggle(True),
        thread.quit()
    })
    worker.show_err_diaglog.connect(show_err_diaglog)

    thread.start()