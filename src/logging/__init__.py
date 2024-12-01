# Desciption: Logger Configuration

from colorama import Fore
from globals import DEBUG_MODE
import logging


class __ColorFormatter(logging.Formatter):
    def format(self, record):
        match record.levelname:
            case "DEBUG":
                # GRAY
                record.msg = Fore.LIGHTBLACK_EX + record.msg + Fore.RESET
                record.levelname = Fore.LIGHTBLACK_EX + record.levelname + Fore.RESET
            case "INFO":
                # BLUE
                record.msg = Fore.BLUE + record.msg + Fore.RESET
                record.levelname = Fore.BLUE + record.levelname + Fore.RESET
            case "WARNING":
                # YELLOW
                record.msg = Fore.YELLOW + record.msg + Fore.RESET
                record.levelname = Fore.YELLOW + record.levelname + Fore.RESET
            case "ERROR":
                # RED
                record.msg = Fore.RED + record.msg + Fore.RESET
                record.levelname = Fore.RED + record.levelname + Fore.RESET
            case "CRITICAL":
                # MAGENTA
                record.msg = Fore.MAGENTA + record.msg + Fore.RESET
                record.levelname = Fore.MAGENTA + record.levelname + Fore.RESET
        return super().format(record)


if DEBUG_MODE:
    __level = logging.DEBUG
    __formatter = logging.Formatter("<%(name)s> [%(levelname)s] %(message)s")
else:
    __level = logging.INFO
    __formatter = logging.Formatter("[%(levelname)s] %(message)s")

__handle_stream = logging.StreamHandler()
__handle_stream.setFormatter(__ColorFormatter(__formatter._fmt))
logging.basicConfig(level=__level, handlers=[__handle_stream])
