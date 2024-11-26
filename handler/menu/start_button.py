from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Menu

# ? Riêng với start_button, sau khi các thông tin trong csv_path, save_path, pptx_path đã được điền đầy đủ, ta sẽ enable nó


def check_start_button(menu: "Menu"):
    '''Kiểm tra với mỗi lần nhập liệu, nếu cả 3 trường đều đã được điền, thì enable start_button'''
    csv_path = menu.csv_path  # noqa: F841
    save_path = menu.save_path  # noqa: F841
    pptx_path = menu.pptx_path  # noqa: F841
    start_button = menu.start_button

    # if csv_path.text() and save_path.text() and pptx_path.text():
    #     start_button.setEnabled(True)
    # else:
    #     start_button.setEnabled(False)
    start_button.setEnabled(True)

def start(menu: "Menu"):
    '''Hàm này sẽ được gọi khi start_button được nhấn'''
    progress = menu.progress

    progress.show()