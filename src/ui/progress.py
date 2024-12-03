from PyQt5 import QtCore, QtWidgets
from PyQt5.QtWidgets import QWidget, QPlainTextEdit
from src.logging.info import console_info
from src.logging.error import console_error
from translations import TRANS

class QTextEditLogger(QtCore.QObject):
    appendPlainText = QtCore.pyqtSignal(str)
    
    # https://stackoverflow.com/a/60528393/16410937
    def __init__(self, parent):
        super().__init__()
        QtCore.QObject.__init__(self)
        self.widget = QPlainTextEdit(parent)
        self.widget.setReadOnly(True)
        self.appendPlainText.connect(self.widget.appendPlainText)

    def append(self, where: str, level: str = "info", title_key: str = None, content: str = "") -> None:
        """
        Adds a log message to the Log.

        Parameters:
        where (str): The location where the log is being added.
        content (str): The content of the log message.
        level (str, optional): The level of the log message. (info, error). Defaults to "info".
        title_key (str, optional): The key to retrieve the title from the TRANS dictionary. Defaults to None.

        Returns:
        None
        """
        if not isinstance(content, str):
            content = str(content)

        match level:
            case "info":
                title = TRANS["progress"]["info"][title_key] if title_key else ""
                console_info(where, title + content)
            case "error":
                title = TRANS["progress"]["error"][title_key] if title_key else ""
                console_error(where, title + content)

        text = title + content
        self.appendPlainText.emit(text)

class Progress(QWidget):
    def __init__(self):
        super().__init__()
        self._setupUi()
        self._retranslateUi()

    def _setupUi(self):
        """
        Set up the user interface.
        """
        self.setObjectName("progress")
        self.resize(574, 374)

        self.__initProgressBar()
        self.__initButtons()
        self.__initLabel()
        self.__initLog()

        QtCore.QMetaObject.connectSlotsByName(self)

    def __initProgressBar(self):
        """
        Initialize the progress bar.
        """
        self.progressBar = QtWidgets.QProgressBar(self)
        self.progressBar.setGeometry(QtCore.QRect(20, 50, 541, 31))
        self.progressBar.setProperty("value", 0)
        self.progressBar.setTextVisible(True)
        self.progressBar.setOrientation(QtCore.Qt.Horizontal)
        self.progressBar.setTextDirection(QtWidgets.QProgressBar.TopToBottom)
        self.progressBar.setObjectName("progressBar")

    def __initButtons(self):
        """
        Initialize the stop and pause buttons.
        """
        self.stop_button = QtWidgets.QPushButton(self)
        self.stop_button.setGeometry(QtCore.QRect(450, 320, 101, 41))
        self.stop_button.setObjectName("stop_button")

        self.pause_button = QtWidgets.QPushButton(self)
        self.pause_button.setGeometry(QtCore.QRect(330, 320, 101, 41))
        self.pause_button.setObjectName("pause_button")

    def __initLabel(self):
        """
        Initialize the label.
        """
        self.label = QtWidgets.QLabel(self)
        self.label.setGeometry(QtCore.QRect(20, 20, 141, 16))
        self.label.setObjectName("label")

    def __initLog(self):
        """
        Initialize the log text edit.
        """
        self.log = QTextEditLogger(self)
        self.log.widget.setGeometry(QtCore.QRect(20, 100, 531, 211))
        self.log.setObjectName("log")

    def _retranslateUi(self):
        """
        Set the text for the UI elements.
        """
        _translate = QtCore.QCoreApplication.translate
        self.setWindowTitle(_translate("progress", "Tiến trình"))
        self.stop_button.setText(_translate("progress", "Dừng"))
        self.pause_button.setText(_translate("progress", "Tạm dừng"))
        self.label.setText(
            _translate(
                "progress",
                '<html><head/><body><p><span style=" font-size:10pt; font-weight:600;">Đang chuẩn bị...</span></p></body></html>',
            )
        )