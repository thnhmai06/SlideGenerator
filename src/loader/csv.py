from typing import TYPE_CHECKING
import polars as pl
from src.logging.info import console_info, default as info
from src.utils.ui.menu.toggle import toggle_config_text, toggle_config_image
from src.utils.ui.menu.clear import clear_config
from src.utils.ui.menu.set import set_csv_loaded_label
from globals import user_input
from translations import TRANS

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

def _create_dataframe_from_csv(csv_path: str) -> bool:
    """
    Mở file CSV và tạo DataFrame từ nó.

    Args:
        csv_path (str): Đường dẫn tới file CSV.

    Returns:
        bool: True nếu đọc thành công, False nếu file CSV không hợp lệ.
    """
    BATCH_SIZE = 10000  # Tiêu hao bộ nhớ rơi vào đâu đó 6.4mb/buffer (10 cột str, 1 cột str url)

    user_input.csv.df = pl.read_csv(csv_path, batch_size=BATCH_SIZE, encoding="utf8-lossy")
    user_input.csv.placeholders = user_input.csv.df.columns
    user_input.csv.number_of_students = len(user_input.csv.df)

    # Trường hợp không có sinh viên nào
    if not user_input.csv.number_of_students >= 1:
        return False
    return True

def load_csv(menu: "Menu"):
    """
    Xử lý file CSV và cập nhật giao diện người dùng.

    Args:
        menu (Menu): Đối tượng Menu chứa các widget giao diện người dùng.
    """
    csv_path = menu.csv_path.text()
    console_info(__name__, TRANS["console"]["info"]["csv_load"], csv_path)

    # Ẩn label trước đó (bằng cách set 0)
    set_csv_loaded_label(menu.csv_loaded, 0, 0)

    # Tạm thời xóa hết config và vô hiệu hóa config_text
    clear_config(menu)
    toggle_config_text(menu, False)

    # Đọc file csv
    loaded = _create_dataframe_from_csv(csv_path)
    if not loaded:
        info(__name__, "csv.no_students")
        return

    # Hiển thị thông tin đã đọc
    set_csv_loaded_label(menu.csv_loaded, len(user_input.csv.placeholders), user_input.csv.number_of_students)
    console_info(__name__, "Fields:", (", ").join(user_input.csv.placeholders), f"({len(user_input.csv.placeholders)})")
    console_info(__name__, "Students:", f"({user_input.csv.number_of_students})")
    
    # Kích hoạt lại config_text
    toggle_config_text(menu, True)

    # Nếu có Shapes ảnh thì kích hoạt config_image
    if len(user_input.shapes) > 0:
        toggle_config_image(menu, True)
