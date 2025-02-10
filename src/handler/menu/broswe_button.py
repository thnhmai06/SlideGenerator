from PyQt5.QtWidgets import QFileDialog
from src.loader.shapes import load_shapes
from src.loader.csv import load_csv
from src.loader.save_path import load_save_path
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

def pptx_broswe(menu: "Menu"):
    """
    Xử lý sự kiện khi nhấn nút Duyệt file PPTX.

    Args:
        menu (Menu): Widget Menu.
    """
    inputLine = menu.pptx_path
    
    file_path, _ = QFileDialog.getOpenFileName(
        menu, "Chọn file Template", "", "PowerPoint File (*.pptx)"
    )

    if file_path:
        inputLine.setText(file_path)
        load_shapes(menu)

def csv_broswe(menu: "Menu"):
    """
    Xử lý sự kiện khi nhấn nút Duyệt file CSV.

    Args:
        menu (Menu): Widget Menu.
    """
    inputLine = menu.csv_path

    file_path, _ = QFileDialog.getOpenFileName(
        menu, "Chọn file DSSV", "", "CSV File (*.csv)"
    )

    if file_path:
        inputLine.setText(file_path)
        load_csv(menu)

def save_path_broswe(menu: "Menu"):
    """
    Xử lý sự kiện khi nhấn nút Duyệt vị trí lưu file.

    Args:
        menu (Menu): Widget Menu.
    """
    inputLine = menu.save_path

    file_path, _ = QFileDialog.getSaveFileName(
        menu, "Chọn vị trí lưu", "", "PowerPoint File (*.pptx)"
    )

    if file_path:
        inputLine.setText(file_path)
        load_save_path(inputLine)
