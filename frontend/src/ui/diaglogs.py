from PyQt5.QtWidgets import QMessageBox
from PyQt5 import QtGui


def show_info(title, message):
    msg = QMessageBox()
    msg.setIcon(QMessageBox.Information)
    windowIcon = QtGui.QIcon()
    windowIcon.addPixmap(QtGui.QPixmap(":/main/uet"), QtGui.QIcon.Normal, QtGui.QIcon.Off)
    msg.setWindowIcon(windowIcon)
    msg.setWindowTitle(title)
    msg.setText(message)
    msg.setStandardButtons(QMessageBox.Ok)
    msg.setMinimumSize(400, 200)  # Set minimum size
    msg.exec_()

def show_warning(title, message):
    msg = QMessageBox()
    msg.setIcon(QMessageBox.Warning)
    windowIcon = QtGui.QIcon()
    windowIcon.addPixmap(QtGui.QPixmap(":/main/uet"), QtGui.QIcon.Normal, QtGui.QIcon.Off)
    msg.setWindowTitle(title)
    msg.setText(message)
    msg.setStandardButtons(QMessageBox.Ok)
    msg.setMinimumSize(400, 200)  # Set minimum size
    msg.exec_()

def show_error(title, message, details=None):
    msg = QMessageBox()
    msg.setIcon(QMessageBox.Critical)
    windowIcon = QtGui.QIcon()
    windowIcon.addPixmap(QtGui.QPixmap(":/main/uet"), QtGui.QIcon.Normal, QtGui.QIcon.Off)
    msg.setWindowTitle(title)
    msg.setText(message)
    msg.setStandardButtons(QMessageBox.Ok)
    if details:
        msg.setDetailedText(details)
    msg.setMinimumSize(400, 200)  # Set minimum size
    msg.exec_()