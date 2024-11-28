from typing import TYPE_CHECKING
from translations import TRANS
from globals import input

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu
    from src.ui.progress import Progress

class LogLevel():
    info = "info"
    error = "error"
__loglevel = LogLevel()

def per_processing(progress: "Progress", num: int):
    progress.add_log(__name__, __loglevel.info, "read_student")
    student = input.csv.get(num)


def exec(progress: "Progress"):
    progress.add_log(__name__, __loglevel.info, "start")
    for num in range(1, input.csv.number_of_students + 1): 
        per_processing(progress, num)
        