import sys
from PyQt5 import QtCore, QtGui, QtWidgets
from PyQt5.QtWidgets import QHeaderView
from src.ui import progress, menu

#? Các hàm hiển thị UI
def Menu():
    ui = menu.Ui()
    ui.show() # Hiển thị UI

def Progress():
    ui = progress.Ui()
    ui.show() # Hiển thị UI


