from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

def clear_config_image_table(menu: "Menu"):
    """Clear config_image_table"""
    menu.config_image_table.clearContents()
    menu.config_image_table.setRowCount(0)


def clear_config_text_list(menu: "Menu"):
    """Clear config_text_list"""
    menu.config_text_list.clear()


def clear_config(menu: "Menu"):
    """Clear config_text_list and config_image_table"""
    clear_config_text_list(menu)
    clear_config_image_table(menu)
