from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Menu


def toggle_config_text(menu: "Menu", is_enable: bool):
    '''Enable the config_text_list, add_button, and remove_button'''
    config_text_list = menu.config_text_list
    add_button = menu.config_text_add_button
    remove_button = menu.config_text_remove_button

    config_text_list.setEnabled(is_enable)
    add_button.setEnabled(is_enable)
    remove_button.setEnabled(is_enable)


def toggle_config_image(menu: "Menu", is_enable: bool):
    '''Enable the config_image_table, add_button, and remove_button'''
    config_image_table = menu.config_image_table
    add_button = menu.config_image_add_button
    remove_button = menu.config_image_remove_button

    config_image_table.setEnabled(is_enable)
    add_button.setEnabled(is_enable)
    remove_button.setEnabled(is_enable)

def clear_config(menu: "Menu"):
    '''Clear config_text_list and config_image_table'''
    config_text_list = menu.config_text_list
    config_image_table = menu.config_image_table

    config_text_list.clear()
    config_image_table.clearContents()