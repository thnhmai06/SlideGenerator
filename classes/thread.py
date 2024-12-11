import sys
from PyQt5.QtCore import QThread, pyqtSignal
from classes.models import PowerPoint
from src.logging.info import default as info
from src.logging.debug import console_debug


def onFinished(powerpoint: PowerPoint):
    try:
        if powerpoint.presentation:
            powerpoint.close_presentation()
        if powerpoint.instance:
            powerpoint.close_instance()
    finally:
        return


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
        self.exit()

    def run(self):
        try:
            # Thử tạo giao thức với PowerPoint
            self.powerpoint.open_instance()
        except Exception as e:
            # Không có Powerpoint
            console_debug(__name__, None, str(e))
            self.logging_no_powerpoint.emit()
            # Ở đây không close Powerpoint vì có mở Powerpoint được đâu
        else:
            # Có Powerpoint
            console_debug(__name__, "powerpoint_found")
        finally:
            self.quit()


class WorkingThread(QThread):
    def __init__(self):
        super().__init__()
        self.powerpoint = PowerPoint()

    def quit(self):
        onFinished(self.powerpoint)
        self.exit()  # Kết thúc Thread
        self.__init__() # Khởi tạo lại Thread cho lần sau
