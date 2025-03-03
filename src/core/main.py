import traceback
import os
from typing import TYPE_CHECKING, List, cast
from PyQt5.QtCore import QObject, pyqtSignal, QMutex, QWaitCondition
from classes.models import PowerPoint, ProgressLogLevel
from classes.threads import ControlledPowerPointThread
from src.logging.error import show_error_diaglog
from src.core.replace import replace_text, replace_image
from src.utils.file import copy_file, delete_file
from src.utils.ui.progress.visible import show_done_button, show_pause_button, disable_controls_button
from globals import user_input, DOWNLOAD_PATH, PROCESSED_PATH, SHAPES_PATH, TEMP_TIME_FOLDER
from translations import get_text, format_text
from src.ui.progress import log_progress

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

SAMPLE_SLIDE_INDEX = 1

class CoreWorker(QObject):
    """
    Lớp xử lý chính cho quá trình thay thế nội dung trong PowerPoint.

    Attributes:
        powerpoint (PowerPoint): Đối tượng PowerPoint để thao tác.
        progress (Progress): Widget Progress để cập nhật trạng thái.
        from_ (int): Chỉ số bắt đầu.
        current (int): Chỉ số hiện tại.
        to_ (int): Chỉ số kết thúc.
        total (int): Tổng số sinh viên cần xử lý.
        is_paused (List[bool]): Trạng thái tạm dừng của Worker.
        is_stopped (List[bool]): Trạng thái dừng của Worker.
        locker (QMutex): Đối tượng khóa để đồng bộ hóa.
        wait_condition (QWaitCondition): Điều kiện chờ để đồng bộ hóa.
    """
    #! Nguyên tắc: Thay đổi UI, ghi ra log cần phải thực hiện trên Thread chính,
    #! đều thông qua tín hiệu để báo ra ngoài Thread chính thay đổi hết.  

    #? Signals
    finished = pyqtSignal() # Tín hiệu khi đã hoàn thành công việc.
    progress_bar_setValue = pyqtSignal(int) # Tín hiệu cập nhật giá trị của progress_bar.
    progress_label_set_label = pyqtSignal(str, tuple) # Tín hiệu cập nhật label trạng thái của progress.
    show_pause_button = pyqtSignal(bool) # Tín hiệu hiển thị nút tạm dừng/tiếp tục.
    show_err_diaglog = pyqtSignal(str, str, str) # Tín hiệu hiển thị cửa sổ thông báo lỗi. 
    disable_controls_button = pyqtSignal() # Tín hiệu vô hiệu hóa các nút điều khiển.
    show_done_button = pyqtSignal() # Tín hiệu hiển thị nút hoàn thành.

    def __init__(self, powerpoint: PowerPoint, progress: "Progress", from_: int, to_: int, 
                 locker: "QMutex", wait_condition: "QWaitCondition", is_paused: List[bool], 
                 is_stopped: List[bool]):
        """
        Khởi tạo đối tượng CoreWorker.

        Args:
            powerpoint (PowerPoint): Đối tượng PowerPoint để thao tác.
            progress (Progress): Widget Progress để cập nhật trạng thái.
            from_ (int): Chỉ số bắt đầu.
            to_ (int): Chỉ số kết thúc.
            locker (QMutex): Đối tượng khóa để đồng bộ hóa.
            wait_condition (QWaitCondition): Điều kiện chờ để đồng bộ hóa.
            is_paused (List[bool]): Trạng thái tạm dừng của thread.
            is_stopped (List[bool]): Trạng thái dừng của thread.
        """
        super().__init__()
        self.powerpoint = powerpoint
        self.progress = progress
        self.from_ = from_
        self.current = int(0)
        self.to_ = to_
        self.total = to_ - from_ + 1

        # Các biến phục vụ tạm dừng và dừng
        self.is_paused = is_paused # Truyền tham chiếu thông qua List - đồng bộ với của Thread
        self.is_stopped = is_stopped # Truyền tham chiếu thông qua List - đồng bộ với của Thread
        self.locker = locker
        self.wait_condition = wait_condition

    def _exception_handler(self, exception: Exception):
        """
        Xử lý ngoại lệ xảy ra trong quá trình chạy.

        Args:
            exception (Exception): Ngoại lệ cần xử lý.
        """
        exception_traceback = traceback.format_exc()
        if isinstance(exception, PermissionError):
            error_message = log_progress(__name__, ProgressLogLevel.ERROR, "permission", error=exception_traceback)
            window_name = get_text("diaglogs.error.window_name")
            self.show_err_diaglog.emit(window_name, error_message, exception_traceback)
        else:
            error_message = log_progress( __name__, ProgressLogLevel.ERROR, "uncaught_exception", error=exception_traceback)
            window_name = get_text("diaglogs.error.window_name")
            self.show_err_diaglog.emit(window_name, f"{error_message}\n\n{str(exception)}", exception_traceback)
    
    def _prepare(self):
        """
        Chuẩn bị các bước cần thiết trước khi bắt đầu xử lý.
        """
        # * Chuẩn bị
        self.progress_label_set_label.emit("preparing", ())

        # Tạo thư mục tạm thời cho phiên làm việc hiện tại
        if not os.path.exists(TEMP_TIME_FOLDER):
            os.makedirs(TEMP_TIME_FOLDER)
            
        # Tạo các thư mục con
        os.makedirs(SHAPES_PATH, exist_ok=True)
        os.makedirs(DOWNLOAD_PATH, exist_ok=True)
        os.makedirs(PROCESSED_PATH, exist_ok=True)

        # Sao chép file gốc sang file lưu
        log_progress(__name__, ProgressLogLevel.INFO, "create_file")
        copy_file(user_input.pptx.path, user_input.save.path)

        # Tạo giao thức với PowerPoint
        log_progress(__name__, ProgressLogLevel.INFO, "open_instance")
        self.powerpoint.open_instance()

        # Mở file PowerPoint
        log_progress(__name__, ProgressLogLevel.INFO, "open_presentation")
        self.powerpoint.open_presentation(user_input.save.path, read_only=False)

        # Thử lưu File
        log_progress(__name__, ProgressLogLevel.INFO, "try_save")
        self.powerpoint.presentation.Save()
        log_progress(__name__, ProgressLogLevel.INFO, "save_path", path=user_input.save.path)

    def _each(self, index: int):
        """
        Xử lý từng sinh viên trong danh sách.

        Args:
            index (int): Chỉ số của sinh viên trong danh sách.
        """
        # Cập nhật label trạng thái
        self.progress_label_set_label.emit("replacing", (str(index), f"({self.current}/{self.total})"))

        # Lấy thông tin sinh viên thứ index
        log_progress(__name__, ProgressLogLevel.INFO, "read_student", num=index)
        student = user_input.csv.get(index)[0]

        # Nhân bản slide
        log_progress(__name__, ProgressLogLevel.INFO, "duplicate_slide")
        slide = self.powerpoint.presentation.Slides(SAMPLE_SLIDE_INDEX).Duplicate()

        # Thay thế Text
        if user_input.config.text:
            replace_text(slide, student)

        # Thay thế Image
        if user_input.config.images:
            replace_image(slide, student, index)

        # Lưu file
        log_progress(__name__, ProgressLogLevel.INFO, "saving")
        self.powerpoint.presentation.Save()

        # Thông báo thay thế sinh viên này thành công, cập nhật thanh progress_bar
        log_progress(__name__, ProgressLogLevel.INFO, "finish_replace")
        self.progress_bar_setValue.emit(self.current)
    def _process(self):
        """
        Bắt đầu quá trình xử lý thay thế.
        """
        # * Bắt đầu
        # Thông báo bắt đầu
        log_progress(__name__, ProgressLogLevel.INFO, "started")
        
        # For lùi để cho các slide khi tạo sẽ đúng theo thứ tự trong file csv
        for count, index in enumerate(range(self.to_, self.from_ - 1, -1), start=1):
            self.current = count

            #* Check Pause 
            self.locker.lock()
            # Tại sao lại dùng while mà không phải if?
            # Vì nếu dùng if thì khi bị wake up, thread sẽ không kiểm tra lại điều kiện
            # mà sẽ tiếp tục chạy xuống dưới, dẫn đến việc bị tạm dừng không hiệu quả
            # (tức là khi đánh thức mà is_paused vẫn True thì chương trình vẫn chạy tiếp)
            while self.is_paused[0]:
                self.progress.status_label.set_label("paused", ())
                self.show_pause_button.emit(False) # Đang bị tạm dừng
                self.wait_condition.wait(self.locker)  # Chờ ở đây đến khi được đánh thức (resume) nếu bị tạm dừng
                self.show_pause_button.emit(True) # Đã được Tiếp tục
            self.locker.unlock()

            #* Check Stop
            if self.is_stopped[0]:
                return

            #* Do work
            self._each(index)

    def _clean_up(self):
        """
        Dọn dẹp những gì đã thực hiện sau khi hoàn thành công việc.
        """
        # * Kết thúc
        self.progress_label_set_label.emit("cleaning", ())

        # Xóa thư mục tạm thời cho phiên làm việc hiện tại
        log_progress(__name__, ProgressLogLevel.INFO, "delete_image_folder")
        delete_file(DOWNLOAD_PATH)
        delete_file(PROCESSED_PATH)

        # Xóa slide đầu tiên (là slide mẫu)
        log_progress(__name__, ProgressLogLevel.INFO, "delete_sample_slide")
        self.powerpoint.presentation.Slides(SAMPLE_SLIDE_INDEX).Delete()
    def _end(self):
        """
        Kết thúc công việc.
        """
        # Đóng file PowerPoint
        log_progress(__name__, ProgressLogLevel.INFO, "close_presentation")
        self.powerpoint.close_presentation(save_before_close=True)

        # Giải phóng môi trường COM
        log_progress(__name__, ProgressLogLevel.INFO, "free_com_environment")
        self.powerpoint.free_com_environment()

        # Thông báo hoàn thành
        self.progress_label_set_label.emit("ended", ())
        log_progress(__name__, ProgressLogLevel.INFO, "ended")

        # Thông báo vị trí lưu file
        log_progress(__name__, ProgressLogLevel.INFO, "save_path", path=user_input.save.path)

    def run(self):
        """
        Chạy công việc
        """
        try:
            self._prepare()
            self._process()
            self._clean_up()
        except Exception as e:
            self._exception_handler(e)
        finally:
            self.disable_controls_button.emit()  # Vô hiệu hóa các nút điều khiển
            self._end()
            self.finished.emit()  # Phát tín hiệu công việc đã kết thúc
            self.show_done_button.emit()  # Hiển thị nút hoàn thành

def work(progress: "Progress", from_: int, to_: int):
    """
    Hàm khởi tạo và bắt đầu quá trình thay thế.

    Args:
        progress (Progress): Widget Progress.
        from_ (int): Chỉ số bắt đầu.
        to_ (int): Chỉ số kết thúc.
    """
    # Đặt giới hạn cho progress_bar
    progress.progress_bar.setMinimum(0)
    progress.progress_bar.setMaximum(to_ - from_ + 1)

    # Tạo thread và worker
    progress.core_thread = cast(type(ControlledPowerPointThread), ControlledPowerPointThread()) # type: ignore
    assert isinstance(progress.core_thread, ControlledPowerPointThread)

    progress.core_worker = cast(type(CoreWorker), CoreWorker( # type: ignore
        powerpoint=progress.core_thread.powerpoint, 
        progress=progress, 
        from_=from_, 
        to_=to_, 
        locker=progress.core_thread.locker, 
        wait_condition=progress.core_thread.wait_condition, 
        is_paused=progress.core_thread.is_paused, 
        is_stopped=progress.core_thread.is_stopped
    ))
    assert isinstance(progress.core_worker, CoreWorker)
    # https://stackoverflow.com/questions/67704387/vscode-using-nonlocal-causes-variable-type-never

    # Chuyển worker vào thread
    progress.core_worker.moveToThread(progress.core_thread)
    progress.core_thread.started.connect(progress.core_worker.run)

    # Nối tín hiệu
    progress.core_worker.progress_bar_setValue.connect(progress.progress_bar.setValue)    
    progress.core_worker.progress_label_set_label.connect(progress.status_label.set_label)
    progress.core_worker.show_err_diaglog.connect(show_error_diaglog)
    progress.core_worker.show_pause_button.connect(lambda is_pause_visible: show_pause_button(progress, is_pause_visible))
    progress.core_worker.disable_controls_button.connect(lambda: disable_controls_button(progress))
    progress.core_worker.show_done_button.connect(lambda: show_done_button(progress))
    
    # Khi kết thúc
    progress.core_worker.finished.connect(lambda: setattr(progress, 'is_finished', True))
    progress.core_worker.finished.connect(progress.core_thread.quit)
    progress.core_worker.finished.connect(progress.core_worker.deleteLater)
    progress.core_thread.finished.connect(progress.core_thread.deleteLater)

    # Bắt đầu chạy
    progress.core_thread.start()