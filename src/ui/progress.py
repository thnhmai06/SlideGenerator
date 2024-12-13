from typing import TYPE_CHECKING
from PyQt5 import QtCore, QtWidgets, QtGui
from PyQt5.QtWidgets import QWidget, QPlainTextEdit, QMessageBox
from functools import reduce
from classes.thread import CoreThread
from src.handler.progress import stop
from src.logging.info import console_info
from src.logging.error import console_error
from src.handler.progress import pause
from translations import TRANS

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

LOGO_PATH = "./assets/logo.png"


def _get_retranslate_window(*args: str) -> str:
    return reduce(lambda d, key: d[key], args, TRANS["progress"]["window"])


class TextEditLogger(QtCore.QObject):
    appendPlainText = QtCore.pyqtSignal(str)

    class LogLevels:
        INFO = "info"
        ERROR = "error"

    # https://stackoverflow.com/a/60528393/16410937
    def __init__(self, parent):
        super().__init__()
        self.widget = QPlainTextEdit(parent)
        self.widget.setReadOnly(True)
        self.appendPlainText.connect(self.widget.appendPlainText)
        self.LogLevels = TextEditLogger.LogLevels
        self.widget.setVerticalScrollBarPolicy(QtCore.Qt.ScrollBarAsNeeded)
        self.widget.setHorizontalScrollBarPolicy(QtCore.Qt.ScrollBarAsNeeded)
        self.widget.setLineWrapMode(QPlainTextEdit.NoWrap)

    def append(
        self, where: str, level: str, title_key: str = None, content: str = ""
    ) -> str:
        """
        Adds a log message to the Log.

        Parameters:
        where (str): The location where the log is being added.
        content (str): The content of the log message.
        level (str, optional): The level of the log message. (info, error). Defaults to "info".
        title_key (str, optional): The key to retrieve the title from the TRANS dictionary. Defaults to None.

        Returns:
        str: The log message.
        """
        if not isinstance(content, str):
            content = str(content)

        match level:
            case self.LogLevels.INFO:
                title = TRANS["progress"]["log"]["info"][title_key] if title_key else ""
                console_info(where, title + content)
            case self.LogLevels.ERROR:
                title = (
                    TRANS["progress"]["log"]["error"][title_key] if title_key else ""
                )
                console_error(where, title + content)

        text = title + content
        self.appendPlainText.emit(text)
        return text


class CloseConfirmWidget(QMessageBox):
    def __init__(self, parent):
        super().__init__(parent)
        icon = QtGui.QIcon()
        icon.addPixmap(QtGui.QPixmap(LOGO_PATH), QtGui.QIcon.Normal, QtGui.QIcon.Off)
        self.setWindowIcon(icon)
        self.setIcon(QMessageBox.Question)
        self.setWindowTitle(_get_retranslate_window("close", "title"))
        self.setText(_get_retranslate_window("close", "message"))
        self.setStandardButtons(QMessageBox.Yes | QMessageBox.No)
        self.setDefaultButton(QMessageBox.No)

    def close_when_not_finished(self):
        reply = self.exec_()
        if reply == QtWidgets.QMessageBox.Yes:
            progress: "Progress" = self.parent()
            stop.exec(progress)

class StatusLabel(QtWidgets.QLabel):
    def __init__(self, parent):
        super().__init__(parent)

    def set_label(self, title_key: str, content: tuple[str, ...]):
        title = TRANS["progress"]["label"][title_key]
        self.setText(f"<b>{title + " ".join(content)}</b>")


class Progress(QWidget):
    def closeEvent(self, event):
        # Ngăn không cho close khi X (đóng cửa sổ)
        if isinstance(event, QtGui.QCloseEvent):
            event.ignore()

        if not self.is_Finished:
            self.close_widget.close_when_not_finished()
        else:
            menu: "Menu" = self.parent_
            super().closeEvent(event)
            self.__init__(menu)
            menu.show()

    def __init__(self, menu):
        super().__init__()
        self.is_Finished = False
        self.parent_ = menu
        self.core_thread = CoreThread()
        self.close_widget = CloseConfirmWidget(self)
        self._setupUi()
        self._retranslateUi()
        self._handleUI()

    def toggle_stop_pause_buttons(self, is_enable: bool):
        self.stop_button.setEnabled(is_enable)
        self.pause_button.setEnabled(is_enable)
        self.resume_button.setEnabled(is_enable)

    def toggle_done_button(self, is_done_visible: bool):
        self.done_button.setVisible(is_done_visible)
        self.stop_button.setVisible(not is_done_visible)
        self.pause_button.setVisible(not is_done_visible)
        self.resume_button.setVisible(not is_done_visible)

    def _setupUi(self):
        """
        Set up the user interface.
        """
        self.setObjectName("progress")
        self.resize(574, 374)
        self.setFixedSize(574, 374)  # Disable resizing

        icon = QtGui.QIcon()
        icon.addPixmap(QtGui.QPixmap(LOGO_PATH), QtGui.QIcon.Normal, QtGui.QIcon.Off)
        self.setWindowIcon(icon)

        self.__initProgressBar()
        self.__initButtons()
        self.__initLabel()
        self.__initLog()

        QtCore.QMetaObject.connectSlotsByName(self)

    def __initProgressBar(self):
        """
        Initialize the progress bar.
        """
        self.progress_bar = QtWidgets.QProgressBar(self)
        self.progress_bar.setGeometry(QtCore.QRect(20, 50, 541, 31))
        self.progress_bar.setProperty("value", 0)
        self.progress_bar.setTextVisible(True)
        self.progress_bar.setOrientation(QtCore.Qt.Horizontal)
        self.progress_bar.setTextDirection(QtWidgets.QProgressBar.TopToBottom)
        self.progress_bar.setObjectName("progress_bar")

    def __initButtons(self):
        """
        Initialize buttons.
        """
        self.stop_button = QtWidgets.QPushButton(self)
        self.stop_button.setGeometry(QtCore.QRect(450, 320, 101, 41))
        self.stop_button.setObjectName("stop_button")

        self.done_button = QtWidgets.QPushButton(self)
        self.done_button.setGeometry(QtCore.QRect(450, 320, 101, 41))
        self.done_button.setObjectName("done_button")
        self.done_button.setVisible(False)

        self.pause_button = QtWidgets.QPushButton(self)
        self.pause_button.setGeometry(QtCore.QRect(330, 320, 101, 41))
        self.pause_button.setObjectName("pause_button")

        self.resume_button = QtWidgets.QPushButton(self)
        self.resume_button.setGeometry(QtCore.QRect(330, 320, 101, 41))
        self.resume_button.setObjectName("resume_button")
        self.resume_button.setVisible(False)

    def __initLabel(self):
        """
        Initialize the label.
        """
        self.status_label = StatusLabel(self)
        self.status_label.setGeometry(QtCore.QRect(20, 20, 531, 16))
        self.status_label.setObjectName("label")

    def __initLog(self):
        """
        Initialize the log text edit.
        """
        self.log = TextEditLogger(self)
        self.log.widget.setGeometry(QtCore.QRect(20, 100, 531, 211))
        self.log.setObjectName("log")

    def _retranslateUi(self):
        """
        Set the text for the UI elements.
        """
        _translate = QtCore.QCoreApplication.translate
        self.setWindowTitle(_translate("progress", _get_retranslate_window("title")))
        self.stop_button.setText(
            _translate("progress", _get_retranslate_window("stop"))
        )
        self.pause_button.setText(
            _translate("progress", _get_retranslate_window("pause"))
        )
        self.resume_button.setText(
            _translate("progress", _get_retranslate_window("resume"))
        )
        self.done_button.setText(
            _translate("progress", _get_retranslate_window("done"))
        )

    def _handleUI(self):
        self.done_button.clicked.connect(lambda: self.close())
        self.pause_button.clicked.connect(lambda: pause.pause(self))
        self.resume_button.clicked.connect(lambda: pause.resume(self))
        self.stop_button.clicked.connect(self.close_widget.close_when_not_finished)

    def show_resume(self):
        self.pause_button.setVisible(False)
        self.resume_button.setVisible(True)

    def show_pause(self):
        self.pause_button.setVisible(True)
        self.resume_button.setVisible(False)

    def finish(self):
        self.toggle_stop_pause_buttons(False)
        self.is_Finished = True
        self.core_thread.quit()
        self.toggle_done_button(True)
