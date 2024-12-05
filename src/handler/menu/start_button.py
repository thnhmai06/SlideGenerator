from typing import TYPE_CHECKING
from src.core.exec import work
from globals import user_input
from src.loader.config import get_config

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

# ? Riêng với start_button, sau khi các thông tin trong csv_path, save_path, pptx_path đã được điền đầy đủ, ta sẽ enable nó


def check_start_button(menu: "Menu"):
    '''Kiểm tra với mỗi lần nhập liệu, nếu đạt điều kiện chạy tối thiểu, thì enable start_button'''
    csv_path = menu.csv_path  # noqa: F841
    save_path = menu.save_path  # noqa: F841
    pptx_path = menu.pptx_path  # noqa: F841
    config_text = menu.config_text_list  # noqa: F841
    config_image = menu.config_image_table  # noqa: F841
    start_button = menu.start_button

    if csv_path.text() and save_path.text() and pptx_path.text() and (config_text.count() > 0 or config_image.rowCount() > 0):
        start_button.setEnabled(True)
    else:
        start_button.setEnabled(False)
    start_button.setEnabled(True)

def handling(menu: "Menu"):
    '''Hàm này sẽ được gọi khi start_button được nhấn'''
    get_config(menu.config_text_list, menu.config_image_table)
    progress = menu.progress

    menu.hide()
    progress.show()    
    work(progress, 1, user_input.csv.number_of_students)