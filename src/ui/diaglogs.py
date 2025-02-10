from PyQt5.QtWidgets import QMessageBox
from PyQt5 import QtGui

LOGO_PATH = "./assets/logo"

def info(title, message):
    """
    Hiển thị hộp thoại thông tin.

    Args:
        title (str): Tiêu đề của hộp thoại.
        message (str): Nội dung thông báo.
    """
    msg = QMessageBox()
    msg.setIcon(QMessageBox.Information)
    windowIcon = QtGui.QIcon()
    windowIcon.addPixmap(
        QtGui.QPixmap(LOGO_PATH), QtGui.QIcon.Normal, QtGui.QIcon.Off
    )
    msg.setWindowIcon(windowIcon)
    msg.setWindowTitle(title)
    msg.setText(message)
    msg.setStandardButtons(QMessageBox.Ok)
    msg.exec_()

def warning(title, message):
    """
    Hiển thị hộp thoại cảnh báo.

    Args:
        title (str): Tiêu đề của hộp thoại.
        message (str): Nội dung cảnh báo.
    """
    msg = QMessageBox()
    msg.setIcon(QMessageBox.Warning)
    windowIcon = QtGui.QIcon()
    windowIcon.addPixmap(
        QtGui.QPixmap(LOGO_PATH), QtGui.QIcon.Normal, QtGui.QIcon.Off
    )
    msg.setWindowIcon(windowIcon)
    msg.setWindowTitle(title)
    msg.setText(message)
    msg.setStandardButtons(QMessageBox.Ok)
    msg.exec_()

def error(title, message, details=None):
    """
    Hiển thị hộp thoại lỗi.

    Args:
        title (str): Tiêu đề của hộp thoại.
        message (str): Nội dung lỗi.
        details (str, optional): Chi tiết lỗi. Mặc định là None.
    """
    msg = QMessageBox()
    msg.setIcon(QMessageBox.Critical)
    windowIcon = QtGui.QIcon()
    windowIcon.addPixmap(
        QtGui.QPixmap(LOGO_PATH), QtGui.QIcon.Normal, QtGui.QIcon.Off
    )
    msg.setWindowIcon(windowIcon)
    msg.setWindowTitle(title)
    msg.setText(message)
    msg.setStandardButtons(QMessageBox.Ok)
    if details:
        msg.setDetailedText(details)
    msg.exec_()
