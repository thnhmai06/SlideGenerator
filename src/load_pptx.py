from PyQt5.QtWidgets import QLineEdit
import os
from globals import SHAPES_PATH

def import_template():
    if not os.path.exists(SHAPES_PATH):
        os.makedirs(SHAPES_PATH)

    # TODO: Download all shapes to SHAPES_PATH here

def load_shapes(inputPath: QLineEdit):
    pptx_path = inputPath.text()

    import_template()
    # TODO: Load shape here

