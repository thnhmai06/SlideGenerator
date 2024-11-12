from PyQt5.QtWidgets import QMessageBox
from PyQt5 import QtGui

def info(title, message):
    msg = QMessageBox()
    msg.setIcon(QMessageBox.Information)
    windowIcon = QtGui.QIcon()
    windowIcon.addPixmap(QtGui.QPixmap("./assets/logo"), QtGui.QIcon.Normal, QtGui.QIcon.Off)
    msg.setWindowIcon(windowIcon)
    msg.setWindowTitle(title)
    msg.setText(message)
    msg.setStandardButtons(QMessageBox.Ok)
    msg.exec_()

def warning(title, message):
    msg = QMessageBox()
    msg.setIcon(QMessageBox.Warning)
    windowIcon = QtGui.QIcon()
    windowIcon.addPixmap(QtGui.QPixmap("./assets/logo"), QtGui.QIcon.Normal, QtGui.QIcon.Off)
    msg.setWindowTitle(title)
    msg.setText(message)
    msg.setStandardButtons(QMessageBox.Ok)
    msg.exec_()

def error(title, message, details=None):
    msg = QMessageBox()
    msg.setIcon(QMessageBox.Critical)
    windowIcon = QtGui.QIcon()
    windowIcon.addPixmap(QtGui.QPixmap("./assets/logo"), QtGui.QIcon.Normal, QtGui.QIcon.Off)
    msg.setWindowTitle(title)
    msg.setText(message)
    msg.setStandardButtons(QMessageBox.Ok)
    if details:
        msg.setDetailedText(details)
    msg.exec_()
