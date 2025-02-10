from typing import TYPE_CHECKING, Optional
from PyQt5 import QtCore, QtWidgets, QtGui
from PyQt5.QtWidgets import QWidget, QPlainTextEdit, QMessageBox
from functools import reduce
from classes.models import ProgressLogLevel
from src.handler import progress as ProgressHandler
from src.logging.info import console_info
from src.logging.error import console_error
from translations import TRANS

if TYPE_CHECKING:
    # Anti-circular import
    from src.ui.menu import Menu

LOGO_PATH = "./assets/logo.png"

def _get_retranslate_window(*args: str) -> str:
    """
    Lấy chuỗi dịch cho cửa sổ từ các khóa trong từ điển dịch: 
    progress -> window -> *args...

    Args:
        *args (str): Các khóa để truy cập vào từ điển dịch.

    Returns:
        str: Chuỗi dịch tương ứng.
    """
    return reduce(lambda d, key: d[key], args, TRANS["progress"]["window"])

class TextEditLogger(QtCore.QObject):
    """
    Lớp quản lý vùng lưu dạng TextEdit cho ProgressLog.

    Attributes:
        appendPlainText (pyqtSignal): Tín hiệu để thêm văn bản vào TextEdit.
        widget (QPlainTextEdit): Widget TextEdit lưu trữ ProgressLog.
    """
    # https://stackoverflow.com/a/60528393/16410937

    appendPlainText = QtCore.pyqtSignal(str)

    def __init__(self, parent):
        """
        Khởi tạo vùng lưu dạng TextEdit cho ProgressLog.

        Args:
            parent: Widget cha.
        """
        super().__init__()
        self.widget = QPlainTextEdit(parent)
        self.widget.setReadOnly(True)
        self.appendPlainText.connect(self.widget.appendPlainText)
        self.widget.setVerticalScrollBarPolicy(QtCore.Qt.ScrollBarAsNeeded)
        self.widget.setHorizontalScrollBarPolicy(QtCore.Qt.ScrollBarAsNeeded)
        self.widget.setLineWrapMode(QPlainTextEdit.NoWrap)

    def append(self, where: str, level: str, title_key: str = None, content: str = "") -> str:
        """
        Thêm một log vào ProgressLog.

        Args:
            where (str): Vị trí thêm log.
            level (str): Mức độ log (info, error).
            title_key (str, optional): Khóa để lấy tiêu đề từ từ điển TRANS. Mặc định là None.
            content (str): Nội dung thông báo log.

        Returns:
            str: Thông báo log.
        """
        if not isinstance(content, str):
            content = str(content)

        match level:
            case ProgressLogLevel.INFO:
                title = TRANS["progress"]["log"]["info"][title_key] if title_key else ""
                console_info(where, title + content)
            case ProgressLogLevel.ERROR:
                title = TRANS["progress"]["log"]["error"][title_key] if title_key else ""
                console_error(where, title + content)

        text = title + content
        self.appendPlainText.emit(text)
        return text
class CloseConfirmWidget(QMessageBox):
    """
    Lớp quản lý widget hộp thoại xác nhận dừng.

    Attributes:
        parent: Widget cha.
    """
    def __init__(self, parent):
        """
        Khởi tạo widget xác nhận dừng.

        Args:
            parent: Widget cha.
        """
        super().__init__(parent)
        icon = QtGui.QIcon()
        icon.addPixmap(QtGui.QPixmap(LOGO_PATH), QtGui.QIcon.Normal, QtGui.QIcon.Off)
        self.setWindowIcon(icon)
        self.setIcon(QMessageBox.Question)
        self.setWindowTitle(_get_retranslate_window("close", "title"))
        self.setText(_get_retranslate_window("close", "message"))
        self.setStandardButtons(QMessageBox.Yes | QMessageBox.No)
        self.setDefaultButton(QMessageBox.No)
class StatusLabel(QtWidgets.QLabel):
    """
    Lớp quản lý label thông báo tiến trình.

    Attributes:
        parent: Widget cha.
    """
    def __init__(self, parent):
        """
        Khởi tạo label thông báo tiến trình.

        Args:
            parent: Widget cha.
        """
        super().__init__(parent)

    def set_label(self, title_key: str, content: tuple[str, ...]):
        """
        Đặt trạng thái tiến trình cho label thông báo.

        Args:
            title_key (str): Khóa tiêu đề.
            content (tuple[str, ...]): Nội dung muốn ghi.
        """
        title = TRANS["progress"]["label"][title_key]
        self.setText(f"<b>{title + ' '.join(content)}</b>")

class Progress(QWidget):
    """
    Lớp quản lý giao diện tiến trình.

    Attributes:
        core_thread (CoreThread): Thread chính để xử lý tiến trình.
        core_worker (CoreWorker): Worker chính để xử lý tiến trình.
        
        parent_ (Menu): Widget Menu cha.
        is_finished (bool): Trạng thái hoàn thành của tiến trình.
        close_widget (CloseConfirmWidget): Widget xác nhận dừng.
        progress_bar (QProgressBar): Thanh tiến trình.
        stop_button (QPushButton): Nút dừng.
        done_button (QPushButton): Nút hoàn thành.
        pause_button (QPushButton): Nút tạm dừng.
        resume_button (QPushButton): Nút tiếp tục.
        status_label (StatusLabel): Nhãn thông báo tiến trình.
        log (TextEditLogger): Vùng lưu log.
    """
    core_thread: Optional[type]
    core_worker: Optional[type]

    is_finished: bool = False

    def closeEvent(self, event):
        """
        Xử lý sự kiện khi đóng cửa sổ.

        Args:
            event: Sự kiện đóng cửa sổ.
        """
        # Ngăn không cho close khi X (đóng cửa sổ)
        if isinstance(event, QtGui.QCloseEvent):
            event.ignore()

        if not self.is_finished:
            ProgressHandler.ask_to_stop(self)
        else:
            menu: "Menu" = self.parent_

            super().closeEvent(event)
            self.__init__(menu)  # Reset lại Progress Widget
            menu.show()

    def __init__(self, menu):
        """
        Khởi tạo Progress.

        Args:
            menu: Menu cha.
        """
        super().__init__()
        self.parent_ = menu
        self.close_widget = CloseConfirmWidget(self)
        self._setupUi()
        self._retranslateUi()
        self._handleUI()

    def _setupUi(self):
        """
        Thiết lập giao diện người dùng.
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
        Thiết lập thanh tiến trình.
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
        Thiết lập các nút.
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
        Thiết lập nhãn.
        """
        self.status_label = StatusLabel(self)
        self.status_label.setGeometry(QtCore.QRect(20, 20, 531, 16))
        self.status_label.setObjectName("label")

    def __initLog(self):
        """
        Thiết lập log text edit.
        """
        self.log = TextEditLogger(self)
        self.log.widget.setGeometry(QtCore.QRect(20, 100, 531, 211))
        self.log.setObjectName("log")

    def _retranslateUi(self):
        """
        Dịch lại các phần tử giao diện người dùng.
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
        """
        Xử lý tương tác giao diện người dùng.
        """
        self.done_button.clicked.connect(lambda: self.close()) # tương tự như Nút X
        self.pause_button.clicked.connect(lambda: ProgressHandler.pause(self))
        self.resume_button.clicked.connect(lambda: ProgressHandler.resume(self))
        self.stop_button.clicked.connect(lambda: ProgressHandler.ask_to_stop(self))
