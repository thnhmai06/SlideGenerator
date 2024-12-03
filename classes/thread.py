import sys
from PyQt5.QtCore import QThread, pyqtSignal
from classes.models import PowerPoint
from src.logging.debug import console_debug
from src.logging.info import default as info

class CheckingThread(QThread):
    no_powerpoint_handler = pyqtSignal()

    def __init__(self):
        super().__init__()
        self.pptx = PowerPoint()
        self.no_powerpoint_handler.connect(
            lambda: {
                info(__name__, "no_powerpoint"),
                sys.exit(1)
            }
        )

    def run(self):
        try:
            # Thử tạo giao thức với PowerPoint
            self.pptx.open_instance()
        except Exception as e:
            # Không có Powerpoint
            console_debug(__name__, None, str(e))
            self.no_powerpoint_handler.emit()
        else: 
            # Có Powerpoint
            console_debug(__name__, "powerpoint_found")
            self.pptx.close_instance()
        finally:
            # Thoát khỏi Thread
            self.quit()