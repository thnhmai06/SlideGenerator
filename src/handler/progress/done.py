from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress
    from src.ui.menu import Menu

def done(progress: "Progress"):
    menu: Menu = progress.parent_
    progress.close()
    progress.__init__(menu)
    menu.show()