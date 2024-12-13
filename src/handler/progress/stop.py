from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

def exec(progress: "Progress"):
    progress.status_label.set_label("stopping", ())
    progress.core_thread.stop()
