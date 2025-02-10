from typing import TYPE_CHECKING
from PyQt5.QtWidgets import QMessageBox

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

def pause(progress: "Progress"):
    """
    Tạm dừng tiến trình.

    Args:
        progress (Progress): Widget Progress.
    """
    progress.status_label.set_label("pausing", ())
    progress.core_thread.pause() # Liên lạc với Thread bảo Tạm dừng

def resume(progress: "Progress"):
    """
    Tiếp tục tiến trình.

    Args:
        progress (Progress): Widget Progress.
    """
    progress.status_label.set_label("resuming", ())
    progress.core_thread.resume() # Liên lạc với Thread bảo Tiếp tục

def ask_to_stop(progress: "Progress"):
    """
    Hỏi để dừng tiến tình, nếu đồng ý thì Dừng tiến trình.

    Args:
        progress (Progress): Widget Progress.
    """
    reply = progress.close_widget.exec()  # Mở hộp thoại hỏi
    if reply == QMessageBox.Yes:
        progress.status_label.set_label("stopping", ())
        progress.core_thread.stop()  # Liên lạc với Thread bảo Dừng