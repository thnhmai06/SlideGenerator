from typing import TYPE_CHECKING
from logger.info import console_info, default as info
from globals import input
from src.loader._get_utils import get_csv
from src.loader._toggle_config import toggle_config_text, toggle_config_image, clear_config

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Menu


def __refresh_shapes():
    # Làm mới placeholders ở local file này
    global __shapes
    __shapes = input.shapes

def load(menu: "Menu"):
    csv_path = menu.csv_path.text()

    # Kiểm tra xem csv_path có tồn tại không (trường hợp Cancel việc chọn file)
    if not csv_path:
        return
    console_info(__name__, "CSV Path:", csv_path)

    
    toggle_config_text(menu, False)
    clear_config(menu)

    is_Csv_vaild = get_csv(
        csv_path
    )  # Thu thập thông tin trong file csv và Chuyển dữ liệu vào dict (ở globals)
    if not is_Csv_vaild:
        info(__name__, "invaild_csv")
        return

    # Now we can enable config_text
    toggle_config_text(menu, True)

    # Nếu có Shapes ảnh thì enable config_image
    __refresh_shapes()
    if len(__shapes) > 0:
        toggle_config_image(menu, True)
