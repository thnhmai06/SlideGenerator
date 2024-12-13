from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

def pause(progress: "Progress"):
    progress.status_label.set_label("pausing", ())
    progress.core_thread.pause()
    # Việc show resume sẽ được xử lý ở trong CoreWorker

def resume(progress: "Progress"):
    progress.status_label.set_label("resuming", ())
    progress.core_thread.resume()
    progress.show_pause()