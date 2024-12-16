import traceback
from typing import TYPE_CHECKING, List
from PyQt5.QtCore import QObject, pyqtSignal, QMutex, QWaitCondition
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
    # Các tín hiệu nối với việc xử lý UI bên ngoài
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

    def __init__(self, pptx: PowerPoint, progress: "Progress", from_: int, to_: int, 
                 locker: "QMutex", wait_condition: "QWaitCondition", is_paused: List[bool], 
                 is_stopped: List[bool]):
        super().__init__()
        # Liệu có xảy ra Đệ quy không?
        # Không, vì đây là truyền tham chiếu
        self.pptx = pptx
        self.progress = progress
        self.from_ = from_
        self.current = 0
        self.to_ = to_
        self.total = to_ - from_ + 1

        # Các biến phục vụ tạm dừng thread
        self.is_paused = is_paused # Truyền tham chiếu thông qua List
        self.is_stopped = is_stopped
        self.locker = locker
        self.wait_condition = wait_condition
    
    def _prepare(self):
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
        self.pptx.open_presentation(user_input.save.path, read_only=False)

    def _each(self, index: int):
        self.progress_label_set_label.emit("replacing", (str(index), f"({self.current}/{self.total})"))

        # Lấy thông tin sinh viên thứ num
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "read_student", index)
        student = user_input.csv.get(index)[0]

        # Nhân bản slide
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "duplicate_slide")
        slide = self.pptx.presentation.Slides(SAMPLE_SLIDE_INDEX).Duplicate()

        # Thay thế Text
        if user_input.config.text:
            replace_text(slide, student, self.progress.log.append, self.progress.log.LogLevels)

        # Thay thế Image
        if user_input.config.image:
            replace_image(slide, student, index, self.progress.log.append, self.progress.log.LogLevels)

        # Lưu file
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "saving")
        self.pptx.presentation.Save()

        # Thông báo thay thế sinh viên này thành công, cập nhật thanh progress_bar
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "finish_replace")
        self.progress_bar_setValue.emit(self.current)

    def _process(self):
        # * Bắt đầu
        self.progress.log.append(__name__, self.progress.log.LogLevels.INFO, "starting")
        # For lùi để cho các slide khi tạo sẽ đúng theo thứ tự trong file csv
        for count, index in enumerate(range(self.to_, self.from_ - 1, -1), start=1):
            self.current = count

            # Check Pause 
            self.locker.lock()
            #* Tại sao lại dùng while mà không phải if?
            # Vì nếu dùng if thì khi bị wake up, thread sẽ không kiểm tra lại điều kiện
            # mà sẽ tiếp tục chạy xuống dưới, dẫn đến việc bị tạm dừng không hiệu quả
            # (tức là khi đánh thức mà is_paused vẫn True thì chương trình vẫn chạy tiếp)
            while self.is_paused[0]:
                self.progress.status_label.set_label("paused", ())
                self.progress.show_resume()
                self.wait_condition.wait(self.locker) # Chờ đến khi được đánh thức (resume) nếu bị tạm dừng
            self.locker.unlock()

            # Check Stop
            if (self.is_stopped[0]):
                return

            # Do work
            self._each(index)

    def _finish(self):
        # * Kết thúc
        self.progress_label_set_label.emit("cleaning", ())

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

    def run(self):
        self._prepare()
        self._process()
        self._finish()
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
    worker = CoreWorker(thread.powerpoint, progress, from_, to_, 
                        thread.locker, thread.wait_condition, thread.is_paused, 
                        thread.is_stopped)
    
    worker.progress_bar_setValue.connect(progress.progress_bar.setValue)    
    worker.progress_label_set_label.connect(progress.status_label.set_label)
    worker.onFinished.connect(progress.finish)
    worker.show_err_diaglog.connect(show_err_diaglog)

    # worker.moveToThread(thread)
    #* Tại sao ở đây lại không connect với thread.started signal mà lại gán trực tiếp thread.run?
    # Vì khi thread.start() được gọi, thread.started phải chờ một thời gian nhất định mới được emit 
    # (unoffical, nhưng thực nghiệm chứng minh là vậy) 
    # Nên là, gán trực tiếp luôn cho thread.run, như vậy sẽ chạy ngay lập tức
    thread.run = worker.start

    thread.start()