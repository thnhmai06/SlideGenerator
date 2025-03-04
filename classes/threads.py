import sys
import pythoncom
from PyQt5.QtCore import QThread, pyqtSignal, QMutex, QWaitCondition
from classes.models import PowerPoint
from src.logging.info import default as info
from src.logging.debug import console_debug

class CheckingThread(QThread):
    """
    Thread kiểm tra PowerPoint đã được cài hay chưa.

    Attributes:
        powerpoint (PowerPoint): Đối tượng PowerPoint chạy thử.
    """
    # ? Logging
    logging_no_powerpoint = pyqtSignal() # Tín hiệu thông báo không tìm thấy PowerPoint.

    def __init__(self):
        super().__init__()
        self.powerpoint = PowerPoint()

        # Nối tín hiệu
        self.logging_no_powerpoint.connect(
            lambda: {info(__name__, "no_powerpoint"), sys.exit(1)}
        )

    def quit(self):
        """
        Kết thúc thread và gọi hàm onFinished.
        """
        self.powerpoint.close_presentation()
        super().quit()  # Kết thúc Thread

    def run(self):
        """
        Chạy thread kiểm tra PowerPoint được cài hay không.
        """
        try:
            # Tạo môi trường COM cho thread
            pythoncom.CoInitialize()

            # Thử tạo giao thức với PowerPoint
            self.powerpoint.open_instance()

            # Thông báo có Powerpoint
            version = self.powerpoint.instance.Version # Nếu ko có powerpoint sẽ xảy ra Expection ở đây
            console_debug(__name__, "powerpoint_found", version)
        except Exception as e:
            # Không có Powerpoint
            console_debug(__name__, None, str(e))
            self.logging_no_powerpoint.emit()
            # Ở đây không close Powerpoint vì có mở Powerpoint được đâu mà đòi đóng
        finally:
            pythoncom.CoUninitialize() # Giải phóng môi trường COM cho thread
            self.quit()

class WorkingThread(QThread):
    """
    Thread làm việc với PowerPoint.

    Attributes:
        powerpoint (PowerPoint): Đối tượng PowerPoint làm việc trong Thread.
    """
    def __init__(self):
        super().__init__()
        self.powerpoint = PowerPoint()

class ControllableThread(WorkingThread):
    """
    Thread làm việc thay thế (là core chương trình), được bổ sung thêm chức năng dừng và tạm dừng.

    Attributes:
        is_paused (bool): Trạng thái tạm dừng của thread.
        is_stopped (bool): Trạng thái dừng của thread.
        locker (QMutex): Đối tượng khóa để đồng bộ hóa.
        wait_condition (QWaitCondition): Điều kiện chờ để đồng bộ hóa.
    """
    def __init__(self):
        super().__init__()
        # Các biến phục vụ cho việc tạm dừng
        self.is_paused = False
        self.is_stopped = False
        self.locker = QMutex()
        self.wait_condition = QWaitCondition()
    
    def pause(self):
        """
        Tạm dừng thread.

        **Cách hoạt động:**
        Khiến giá trị is_paused thành True, khi đó vòng for trong worker 
        khi đến lần lặp tiếp theo sẽ tạm dừng lại và "ngủ".
        """
        self.locker.lock()
        self.is_paused = True
        self.locker.unlock()
        # Tiếp tục trên Worker

    def resume(self):
        """
        Tiếp tục thread.
        
        **Cách hoạt động:**
        Đặt giá trị is_paused thành False và đánh thức các thread đang "ngủ".
        """
        self.locker.lock()
        self.is_paused = False
        self.wait_condition.wakeAll()
        self.locker.unlock()
        # Tiếp tục trên Worker

    def stop(self):
        """
        Dừng thread.
        
        **Cách hoạt động:**
        Đặt giá trị is_stopped thành True và cho tiếp tục chạy.
        Vòng for trong worker sẽ được tiếp tục và sẽ kiểm tra is_stopped, 
        do nó đang là True nên sẽ dừng ngay lại.
        """
        self.locker.lock()
        self.is_stopped = True
        self.locker.unlock()
        
        if (self.is_paused):
            self.resume()
        # Tiếp tục trên Worker