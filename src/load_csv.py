from PyQt5.QtWidgets import QLineEdit, QListWidget, QPushButton, QWidget
from globals import import_csv_fields, placeholders
from handle.config_text import add_item

def loadPlaceholders(ui: QWidget):
    inputPath = ui.dssv_path
    config_text_list = ui.config_text_list
    add_button = ui.config_text_add_button
    remove_button = ui.config_text_remove_button
    csv_path = inputPath.text()

    print("CSV Path: ", csv_path)

    if not csv_path:
        return
    
    # Load fields from csv file
    import_csv_fields(csv_path)
    print("CSV Fields: ", end="")
    for placeholder in placeholders:
        print(placeholder, end='\t')
    print() # New line

    # Add 1 item to list
    add_item(config_text_list)

    # Enable the config_text_list, add_button, and remove_button
    config_text_list.setEnabled(True)
    add_button.setEnabled(True)
    remove_button.setEnabled(True)