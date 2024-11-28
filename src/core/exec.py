from typing import TYPE_CHECKING
from globals import input

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.progress import Progress

class LogLevel():
    info = "info"
    error = "error"
__loglevel = LogLevel()

def per_processing(progress: "Progress", num: int):
    progress.add_log(__name__, __loglevel.info, "read_student")
    student = input.csv.get(num)


def exec(progress: "Progress", from_: int = 1, to_: int = input.csv.number_of_students):
    progress.add_log(__name__, __loglevel.info, "start")
    for num in range(from_, to_ + 1): 
        per_processing(progress, num)
