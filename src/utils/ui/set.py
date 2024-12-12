from PyQt5.QtWidgets import QLabel
from translations import TRANS


def set_pptx_loaded_label(label: QLabel, num: int = 0):
    """Enable the loaded_label"""
    if num == 0:
        label.setVisible(False)
        return
    text: str = TRANS["menu"]["window"]["pptx"]["loaded"]
    text = text.format(shapes=num)
    label.setText(text)
    label.setVisible(True)


def set_csv_loaded_label(label: QLabel, fields: int = 0, students: int = 0):
    """Enable the loaded_label"""
    if fields == 0 or students == 0:
        label.setVisible(False)
        return
    text: str = TRANS["menu"]["window"]["csv"]["loaded"]
    text = text.format(fields=fields, students=students)
    label.setText(text)
    label.setVisible(True)
