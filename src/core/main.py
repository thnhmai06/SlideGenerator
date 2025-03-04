import os
import traceback
from typing import TYPE_CHECKING, cast, Optional, Dict, Any
from PyQt5.QtCore import QObject, pyqtSignal
from classes.models import PowerPoint, ProgressLogLevel
from classes.threads import ControllableThread
from src.core.replace import replace_text, replace_image
from src.utils.file import copy_file, delete_file
from src.utils.ui.progress.visible import show_done_button, show_pause_button, disable_controls_button
from src.ui.progress import log_progress
from src.ui.diaglogs import error as show_error_diaglog
from globals import user_input, DOWNLOAD_PATH, PROCESSED_PATH, TEMP_TIME_FOLDER

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

SAMPLE_SLIDE_INDEX = 1

class CoreWorker(QObject):
    """
    Lớp xử lý chính cho quá trình thay thế nội dung trong PowerPoint.

    Attributes:
        powerpoint (PowerPoint): Đối tượng PowerPoint để thao tác.
        from_ (int): Chỉ số bắt đầu.
        current (int): Chỉ số hiện tại.
        to_ (int): Chỉ số kết thúc.
        total (int): Tổng số sinh viên cần xử lý.
        _thread (Optional[ControllableThread]): Đối tượng Thread chịu trách nhiệm chạy worker.
    """
    #! Nguyên tắc: Thay đổi UI, ghi ra log cần phải thực hiện trên Thread chính,
    #! đều thông qua tín hiệu để báo ra ngoài Thread chính thay đổi hết.  

    # Signals
    finished = pyqtSignal() 
    progress_bar_setValue = pyqtSignal(int) 
    progress_label_set_label = pyqtSignal(str, tuple) 
    show_pause_button = pyqtSignal(bool) 
    show_error_diaglog = pyqtSignal(str, str) 
    disable_controls_button = pyqtSignal() 
    show_done_button = pyqtSignal() 

    def __init__(self, powerpoint: PowerPoint, from_: int, to_: int):
        """
        Khởi tạo đối tượng CoreWorker.

        Args:
            powerpoint (PowerPoint): Đối tượng PowerPoint để thao tác.
            from_ (int): Chỉ số bắt đầu.
            to_ (int): Chỉ số kết thúc.
        """
        super().__init__()
        self.powerpoint = powerpoint
        self.from_ = from_
        self.current = int(0)
        self.to_ = to_
        self.total = to_ - from_ + 1
        self._thread: Optional[ControllableThread] = None
        
    def _exception_handler(self, exception: Exception) -> None:
        """
        Xử lý ngoại lệ xảy ra trong quá trình chạy.

        Args:
            exception (Exception): Ngoại lệ cần xử lý.
        """
        exception_traceback = traceback.format_exc()

        if isinstance(exception, PermissionError):
            error_message = log_progress(__name__, ProgressLogLevel.ERROR, "permission", error=exception_traceback)
            self.show_error_diaglog.emit(error_message, exception_traceback)
        else:
            error_message = log_progress(__name__, ProgressLogLevel.ERROR, "uncaught_exception", error=exception_traceback)
            self.show_error_diaglog.emit(f"{error_message}\n\n{str(exception)}", exception_traceback)

    def _check_pause_and_stop(self) -> bool:
        """
        Kiểm tra và xử lý tạm dừng hoặc dừng hẳn quá trình.
        
        Returns:
            bool: Quá trình có bị dừng hẳn không.
        """
        if not self._thread:
            return False
            
        # Check Pause 
        while self._thread.is_paused:
            self.progress_label_set_label.emit("paused", ())
            self.show_pause_button.emit(False)
            self._thread.locker.lock()
            self._thread.wait_condition.wait(self._thread.locker)
            self._thread.locker.unlock()
            self.show_pause_button.emit(True)

        # Check Stop
        return self._thread.is_stopped

    def _create_working_directories(self) -> None:
        """Tạo các thư mục cần thiết cho quá trình xử lý."""
        # Tạo thư mục tạm thời cho phiên làm việc hiện tại
        if not os.path.exists(TEMP_TIME_FOLDER):
            os.makedirs(TEMP_TIME_FOLDER)
            
        # Tạo các thư mục con
        os.makedirs(DOWNLOAD_PATH, exist_ok=True)
        os.makedirs(PROCESSED_PATH, exist_ok=True)

    def _delete_working_directories(self) -> None:
        """Xóa các thư mục tạm thời đã tạo."""
        delete_file(DOWNLOAD_PATH)
        delete_file(PROCESSED_PATH)

    def _prepare(self) -> None:
        """
        Chuẩn bị các bước cần thiết trước khi bắt đầu xử lý.
        """
        self.progress_label_set_label.emit("preparing", ())

        # Tạo cấu trúc thư mục
        self._create_working_directories()
            
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

        # Thông báo vị trí lưu file
        log_progress(__name__, ProgressLogLevel.INFO, "save_path", path=user_input.save.path)

    def _replace_slide_content(self, slide: Any, student: Dict[str, str], index: int) -> None:
        """
        Thực hiện thay thế nội dung trong slide.
        
        Args:
            slide: Slide cần thay thế nội dung
            student: Thông tin sinh viên
            index: Chỉ số của sinh viên
        """
        # Thay thế Text
        if user_input.config.text:
            replace_text(slide, student)

        # Thay thế Image
        if user_input.config.images:
            replace_image(slide, student, index)

    def _each(self, index: int) -> None:
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

        # Thay thế nội dung slide
        self._replace_slide_content(slide, student, index)

        # Lưu file
        log_progress(__name__, ProgressLogLevel.INFO, "saving")
        self.powerpoint.presentation.Save()

        # Thông báo thay thế sinh viên này thành công, cập nhật thanh progress_bar
        log_progress(__name__, ProgressLogLevel.INFO, "finish_replace")
        self.progress_bar_setValue.emit(self.current)
            
    def _process(self) -> None:
        """
        Bắt đầu quá trình xử lý thay thế.
        """
        # Thông báo bắt đầu
        log_progress(__name__, ProgressLogLevel.INFO, "started")
        
        # For lùi để cho các slide khi tạo sẽ đúng theo thứ tự trong file csv
        for count, index in enumerate(range(self.to_, self.from_ - 1, -1), start=1):
            self.current = count

            # Kiểm tra trạng thái tạm dừng và dừng
            if self._check_pause_and_stop():
                return

            # Xử lý từng sinh viên
            self._each(index)

    def _clean_up(self) -> None:
        """
        Dọn dẹp những gì đã thực hiện sau khi hoàn thành công việc.
        """
        self.progress_label_set_label.emit("cleaning", ())

        # Xóa slide đầu tiên (là slide mẫu)
        log_progress(__name__, ProgressLogLevel.INFO, "delete_sample_slide")
        self.powerpoint.presentation.Slides(SAMPLE_SLIDE_INDEX).Delete()

        # Xóa thư mục tạm thời cho phiên làm việc hiện tại
        log_progress(__name__, ProgressLogLevel.INFO, "delete_image_folder")
        self._delete_working_directories()
        
    def _end(self) -> None:
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
        log_progress(__name__, ProgressLogLevel.INFO, "ended")
        self.progress_label_set_label.emit("ended", ())

        # Thông báo vị trí lưu file
        log_progress(__name__, ProgressLogLevel.INFO, "save_path", path=user_input.save.path)

    def run(self) -> None:
        """
        Chạy công việc chính
        """
        self._thread = cast(ControllableThread, self.thread())

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

def work(progress: "Progress", from_: int, to_: int) -> None:
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
    progress.core_thread = cast(ControllableThread, ControllableThread())
    assert isinstance(progress.core_thread, ControllableThread)
    progress.core_worker = cast(CoreWorker, 
        CoreWorker(
            powerpoint=progress.core_thread.powerpoint, 
            from_=from_, 
            to_=to_
        )
    )
    assert isinstance(progress.core_worker, CoreWorker)

    # Chuyển worker vào thread
    progress.core_worker.moveToThread(progress.core_thread)
    progress.core_thread.started.connect(progress.core_worker.run)

    # Nối tín hiệu
    progress.core_worker.progress_bar_setValue.connect(progress.progress_bar.setValue)    
    progress.core_worker.progress_label_set_label.connect(progress.status_label.set_label)
    progress.core_worker.show_error_diaglog.connect(
        lambda message, details: show_error_diaglog(message=message, details=details)
    )
    progress.core_worker.show_pause_button.connect(
        lambda is_pause_visible: show_pause_button(progress, is_pause_visible)
    )
    progress.core_worker.disable_controls_button.connect(lambda: disable_controls_button(progress))
    progress.core_worker.show_done_button.connect(lambda: show_done_button(progress))
    
    # Khi kết thúc
    progress.core_worker.finished.connect(lambda: setattr(progress, 'is_finished', True))
    progress.core_worker.finished.connect(progress.core_thread.quit)
    progress.core_worker.finished.connect(progress.core_worker.deleteLater)
    progress.core_thread.finished.connect(progress.core_thread.deleteLater)

    # Bắt đầu chạy
    progress.core_thread.start()