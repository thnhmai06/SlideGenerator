from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui

# ? Riêng với start_button, sau khi các thông tin trong csv_path, save_path, pptx_path đã được điền đầy đủ, ta sẽ enable nó


# Kiểm tra với mỗi lần nhập liệu, nếu cả 3 trường đều đã được điền, thì enable start_button
def check_start_button(ui: "Ui"):
    csv_path = ui.csv_path
    save_path = ui.save_path
    pptx_path = ui.pptx_path
    start_button = ui.start_button

    if csv_path.text() and save_path.text() and pptx_path.text():
        start_button.setEnabled(True)
    else:
        start_button.setEnabled(False)


def start():
    # Hàm này sẽ được gọi khi start_button được nhấn
    None
