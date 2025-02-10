from typing import TYPE_CHECKING
from globals import user_input
from src.core.main import work
from src.loader.config import load_config

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

def check_start_button(menu: "Menu"):
    """
    Kiểm tra với mỗi lần nhập liệu, nếu đạt điều kiện chạy tối thiểu, thì enable start_button.

    **Điều kiện chạy tối thiểu:**
    - Đã điền File pptx mẫu
    - Đã điền File csv
    - Đã điền vị trí lưu
    - Đã điền ít nhất một cấu hình text hoặc image.

    Args:
        menu (Menu): Widget Menu.
    """
    csv_path = menu.csv_path
    save_path = menu.save_path
    pptx_path = menu.pptx_path
    config_text = menu.config_text_list
    config_image = menu.config_image_table 
    start_button = menu.start_button

    if csv_path.text() and save_path.text() and pptx_path.text() and (config_text.count() > 0 or config_image.rowCount() > 0):
        start_button.setEnabled(True)
    else:
        start_button.setEnabled(False)

def exec(menu: "Menu"):
    """
    Hàm này sẽ được gọi khi start_button được nhấn.

    Args:
        menu (Menu): Widget Menu.
    """
    progress = menu.progress

    # Load Text Config và Image Config vào user_input
    load_config(menu.config_text_list, menu.config_image_table)

    # Ẩn Menu và Hiện Progress
    menu.hide()
    progress.show()

    # Bắt đầu công việc
    work(
        progress=progress, 
        from_=1, 
        to_=user_input.csv.number_of_students
    )