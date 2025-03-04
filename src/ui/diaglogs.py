from PyQt5.QtWidgets import QMessageBox
from PyQt5 import QtGui
from translations import get_text

LOGO_PATH = "./assets/logo"

def info(title: str = None, message: str = None):
    """
    Hiển thị hộp thoại thông tin.

    Args:
        title (str, optional): Tiêu đề của hộp thoại. Mặc định là None.
        message (str, optinal): Nội dung thông báo.
    """
    if title is None:
        title = get_text('diaglogs.information.window_name')
    if message is None:
        message = get_text('diaglogs.information.default')
    
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

def error(title: str = None, message: str = None, details: str = None):

    """
    Hiển thị hộp thoại lỗi.

    Args:
        title (str, optional): Tiêu đề của hộp thoại. Mặc định là None.
        message (str): Nội dung lỗi.
        details (str, optional): Chi tiết lỗi. Mặc định là None.
    """
    if title is None:
        title = get_text('diaglogs.error.window_name')
    if message is None:
        message = get_text('diaglogs.error.default')

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
