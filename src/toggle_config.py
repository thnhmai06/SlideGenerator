from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui


def toggle_config_text(ui: "Ui", is_enable: bool):
    # Enable the config_text_list, add_button, and remove_button
    config_text_list = ui.config_text_list
    add_button = ui.config_text_add_button
    remove_button = ui.config_text_remove_button

    config_text_list.setEnabled(is_enable)
    add_button.setEnabled(is_enable)
    remove_button.setEnabled(is_enable)


def toggle_config_image(ui: "Ui", is_enable: bool):
    # Enable the config_image_table, add_button, and remove_button
    config_image_table = ui.config_image_table
    add_button = ui.config_image_add_button
    remove_button = ui.config_image_remove_button

    config_image_table.setEnabled(is_enable)
    add_button.setEnabled(is_enable)
    remove_button.setEnabled(is_enable)
