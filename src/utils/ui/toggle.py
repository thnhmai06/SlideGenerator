from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

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
