from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

def disable_controls_button(progress: "Progress"):
    """
    Vô hiệu hóa các nút Controls (Dừng, Tạm dừng, Tiếp tục).
    """
    progress.pause_button.setEnabled(False)
    progress.resume_button.setEnabled(False)
    progress.stop_button.setEnabled(False)

def show_done_button(progress: "Progress"):
    """
    Hiển thị nút Hoàn thành và ẩn các nút khác.
    """
    progress.stop_button.setVisible(False)
    progress.pause_button.setVisible(False)
    progress.resume_button.setVisible(False)

    progress.done_button.setVisible(True)

def show_pause_button(progress: "Progress", is_pause_visible: bool = True):
    """
    Hiển thị nút tạm dừng/tiếp tục.

    Args:
        is_pause_visible (bool): True sẽ hiển thị nút Tạm dừng, False sẽ hiển thị nút Tiếp tục. Mặc định là True.
    """
    progress.pause_button.setVisible(is_pause_visible)
    progress.resume_button.setVisible(not is_pause_visible)