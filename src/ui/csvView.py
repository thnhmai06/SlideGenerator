import sys
from PyQt5.QtWidgets import QDialog, QVBoxLayout, QTextEdit, QPushButton
from PyQt5 import QtGui

class Ui(QDialog):
    def __init__(self, array, parent=None):
        super(Ui, self).__init__(parent)
        self.setWindowTitle("Các Placeholder")
        icon = QtGui.QIcon()
        icon.addPixmap(QtGui.QPixmap(":/main/uet"), QtGui.QIcon.Normal, QtGui.QIcon.Off)
        self.setWindowIcon(icon)
        layout = QVBoxLayout()

        self.text_edit = QTextEdit()
        self.text_edit.setReadOnly(True)
        self.text_edit.setPlainText("\n".join(array))
        layout.addWidget(self.text_edit)

        self.setLayout(layout)