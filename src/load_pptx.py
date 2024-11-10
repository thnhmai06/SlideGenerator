from PyQt5.QtWidgets import QLineEdit
from globals import import_template

def loadShapes(inputPath: QLineEdit):
    # @params: inputPath: QLineEdit
    template_path = inputPath.text()

    import_template()

    # Image lưu ở shapesPath
    # TODO: Load shape here
    print("Load Shape Here!!")

