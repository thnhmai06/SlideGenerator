from PyQt5.QtWidgets import QLineEdit, QListWidget, QPushButton, QWidget
from typing import TYPE_CHECKING
import pandas as pd
import os
from globals import placeholders
from logger.information import _console_info
from handle import config_text

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui

def __import_csv_fields(csv_path: str) -> None:
    _console_info("Import CSV: ", os.path.dirname(csv_path))
    df = pd.read_csv(csv_path)
    fields = df.columns.tolist()
    for field in fields:
        placeholders.append(field)
    _console_info("Fields: ", " | ".join(placeholders), "(*end)")

def load_placeholders(ui: 'Ui'):
    csv_path = ui.csv_path.text()
    config_text_list = ui.config_text_list
    add_button = ui.config_text_add_button
    remove_button = ui.config_text_remove_button

    if not csv_path:
        return
    
    # Load fields from csv file
    __import_csv_fields(csv_path)

    # Enable the config_text_list, add_button, and remove_button
    config_text_list.setEnabled(True)
    add_button.setEnabled(True)
    remove_button.setEnabled(True)