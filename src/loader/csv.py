from typing import TYPE_CHECKING
import polars as pl
from src.logging.info import console_info, default as info
from src.utils.ui.toggle import toggle_config_text, toggle_config_image
from src.utils.ui.clear import clear_config
from src.utils.ui.set import set_csv_loaded_label
from globals import user_input
from translations import TRANS

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

def _read_data(csv_path: str) -> bool:
    """
    Return:
    - True: Saved successfully
    - False: CSV is not valid
    """
    LINES_PER_BATCH = 1

    user_input.csv.df = pl.read_csv(csv_path, batch_size=LINES_PER_BATCH, encoding="utf8-lossy")
    user_input.csv.placeholders = user_input.csv.df.columns
    user_input.csv.number_of_students = len(user_input.csv.df)

    # Trường hợp không có sinh viên nào
    if not user_input.csv.number_of_students >= 1:
        return False
    return True

def process_csv(menu: "Menu"):
    csv_path = menu.csv_path.text()

    set_csv_loaded_label(menu.csv_loaded, 0, 0)
    console_info(__name__, TRANS["console"]["info"]["csv_load"], csv_path)

    toggle_config_text(menu, False)
    clear_config(menu)

    is_Csv_vaild = _read_data(csv_path)  # Thu thập thông tin trong file csv và Chuyển dữ liệu vào dict (ở globals)
    if not is_Csv_vaild:
        info(__name__, "csv.no_students")
        return

    # Hiển thị thông tin đã đọc
    set_csv_loaded_label(menu.csv_loaded, len(user_input.csv.placeholders), user_input.csv.number_of_students)
    console_info(__name__, "Fields:", (", ").join(user_input.csv.placeholders), f"({len(user_input.csv.placeholders)})")
    console_info(__name__, "Students:", f"({user_input.csv.number_of_students})")
    # Now we can enable config_text
    toggle_config_text(menu, True)

    # Nếu có Shapes ảnh thì enable config_image
    if len(user_input.shapes) > 0:
        toggle_config_image(menu, True)
