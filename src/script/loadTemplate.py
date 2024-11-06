from PyQt5.QtWidgets import QLineEdit
from globals import *

def loadShapes(inputPath: QLineEdit):
    # @params: inputPath: QLineEdit
    template_path = inputPath.text()

    importTemplate()

    # Image lưu ở shapesPath
    # TODO: Load shape here
    print("Load Shape Here!!")

