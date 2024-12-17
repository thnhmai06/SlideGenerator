import sys
import pythoncom
from typing import Callable
from PyQt5.QtCore import QThread, pyqtSignal, QMutex, QWaitCondition
from classes.models import PowerPoint
from src.logging.info import default as info
from src.logging.debug import console_debug


def onFinished(powerpoint: PowerPoint):
    if powerpoint.presentation:
        try:
            powerpoint.presentation.Save()
            powerpoint.close_presentation()
        except Exception:
            pass


class CheckingThread(QThread):
    # ? Logging
    logging_no_powerpoint = pyqtSignal()

    def __init__(self):
        super().__init__()
        self.powerpoint = PowerPoint()

        # ? Logging Connection
        self.logging_no_powerpoint.connect(
            lambda: {info(__name__, "no_powerpoint"), sys.exit(1)}
        )

    def quit(self):
        onFinished(self.powerpoint)
        super().quit()  # Kết thúc Thread

    def run(self):
        try:
            # Tạo môi trường COM cho thread
            pythoncom.CoInitialize()

            # Thử tạo giao thức với PowerPoint
            self.powerpoint.open_instance()

            # Thông báo có Powerpoint
            version = self.powerpoint.instance.Version #! Nếu ko có powerpoint sẽ lỗi ở đây
            console_debug(__name__, "powerpoint_found", version)
        except Exception as e:
            # Không có Powerpoint
            console_debug(__name__, None, str(e))
            self.logging_no_powerpoint.emit()
            # Ở đây không close Powerpoint vì có mở Powerpoint được đâu mà đòi đóng
        finally:
            # Tạo môi trường COM cho thread
            pythoncom.CoUninitialize()
            self.quit()

class WorkingThread(QThread):
    def __init__(self):
        super().__init__()
        self.powerpoint = PowerPoint()

    def quit(self, next: Callable = None):
        #* Hàm này dừng việc EventLoop trong CoreThread, không phải dừng hàm run trong CoreThread ngay
        onFinished(self.powerpoint)
        if next:
            next()
        super().quit()  # Kết thúc Thread
        self.__init__() # Khởi tạo lại Thread cho lần sau

class CoreThread(WorkingThread):
    # Giống WorkingThread nhưng có chức năng tạm dừng, dừng
    # Logic tạm dừng được thực hiện trong CoreWorker
    def __init__(self):
        super().__init__()
        # Các biến phục vụ cho việc tạm dừng
        self.is_paused = [False] # Dùng để truyền tham chiếu cho CoreWorker thông qua List
        self.is_stopped = [False]
        self.locker = QMutex()
        self.wait_condition = QWaitCondition()
    
    def pause(self):
        self.locker.lock()
        self.is_paused[0] = True
        self.locker.unlock()

    def resume(self):
        self.locker.lock()
        self.is_paused[0] = False
        self.wait_condition.wakeAll()
        self.locker.unlock()

    def stop(self):
        self.locker.lock()
        self.is_stopped[0] = True
        self.locker.unlock()
        if (self.is_paused[0]):
            self.resume()