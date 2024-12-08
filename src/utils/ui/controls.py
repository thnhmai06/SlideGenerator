from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu


# ? Toggle
def toggle_config_text(menu: "Menu", enable: bool):
    """Enable the config_text_list, add_button, and remove_button"""
    config_text_list = menu.config_text_list
    add_button = menu.config_text_add_button
    remove_button = menu.config_text_remove_button

    config_text_list.setEnabled(enable)
    add_button.setEnabled(enable)
    remove_button.setEnabled(enable)


def toggle_config_image(menu: "Menu", enable: bool):
    """Enable the config_image_table, add_button, and remove_button"""
    config_image_table = menu.config_image_table
    add_button = menu.config_image_add_button
    remove_button = menu.config_image_remove_button

    config_image_table.setEnabled(enable)
    add_button.setEnabled(enable)
    remove_button.setEnabled(enable)


# ? Clear
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
