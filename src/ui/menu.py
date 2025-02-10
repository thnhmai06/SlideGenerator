from functools import reduce
from typing import Optional
from PyQt5 import QtCore, QtGui
from PyQt5.QtWidgets import (
    QMainWindow,
    QWidget,
    QLabel,
    QGroupBox,
    QPushButton,
    QLineEdit,
    QListWidget,
    QTableWidget,
    QTableWidgetItem,
    QHeaderView,
    QStatusBar,
)
from src.ui.progress import Progress
from src.handler.menu import config_image, start_button
from src.handler.menu import broswe_button, config_text
from src.handler.menu.start_button import check_start_button
from globals import GITHUB_URL
from translations import TRANS

LOGO_PATH = "./assets/logo"
ADD_ICON_PATH = "./assets/button/add"
REMOVE_ICON_PATH = "./assets/button/remove"
GITHUB_ICON_PATH = "./assets/button/github"
GUIDE_ICON_PATH = "./assets/button/guide"
ABOUT_ICON_PATH = "./assets/button/about"

def _get_retranslate_window(*args: str) -> str:
    """
    Lấy chuỗi dịch cho cửa sổ từ các khóa trong từ điển dịch.

    Args:
        *args (str): Các khóa để truy cập vào từ điển dịch.

    Returns:
        str: Chuỗi dịch tương ứng.
    """
    return reduce(lambda d, key: d[key], args, TRANS["menu"]["window"])

class Menu(QMainWindow):
    """
    Lớp quản lý giao diện Menu chính.

    Attributes:
        load_shapes_thread (WorkingThread): Thread để tải các hình ảnh.
        load_shapes_worker (WorkingWorker): Worker để tải các hình ảnh.

        centralwidget (QWidget): Widget trung tâm.
        title (QLabel): Tiêu đề của cửa sổ.
        pptx (QGroupBox): Nhóm chứa thông tin về file PPTX.
        csv (QGroupBox): Nhóm chứa thông tin về file CSV.
        save (QGroupBox): Nhóm chứa thông tin về đường dẫn lưu file.
        config (QGroupBox): Nhóm chứa thông tin cấu hình.
        start_button (QPushButton): Nút bắt đầu.
        logo (QLabel): Logo của ứng dụng.
        github (QLabel): Biểu tượng GitHub.
        about (QLabel): Biểu tượng About.
        guide (QLabel): Biểu tượng Guide.
        statusbar (QStatusBar): Thanh trạng thái.
        progress (Progress): Widget Progress.
    """
    load_shapes_thread: Optional[type]  # Slot cho Thread để load shapes
    load_shapes_worker: Optional[type]  # Slot cho Worker để load shapes

    def closeEvent(self, a0):
        """
        Xử lý sự kiện khi đóng cửa sổ.

        Args:
            a0: Sự kiện đóng cửa sổ.
        """
        from src.utils.file import delete_file
        from globals import SHAPES_PATH
        from src.logging.debug import console_debug

        console_debug(__name__, "delete_shapes", SHAPES_PATH)
        delete_file(SHAPES_PATH)
        return super().closeEvent(a0)

    def __init__(self):
        """
        Khởi tạo cửa sổ Menu.
        """
        super().__init__()
        self._setupUi()
        self._retranslateUi()
        self._handleUI()
        self.progress = Progress(self)

    def _setupUi(self):
        """
        Thiết lập giao diện người dùng.
        """
        self.setObjectName("menu")
        self.resize(959, 867)
        self.setMinimumSize(QtCore.QSize(959, 867))
        self.setMaximumSize(QtCore.QSize(959, 867))
        self.setCursor(QtGui.QCursor(QtCore.Qt.ArrowCursor))
        self.setMouseTracking(False)

        icon = QtGui.QIcon()
        icon.addPixmap(QtGui.QPixmap(LOGO_PATH), QtGui.QIcon.Normal, QtGui.QIcon.Off)
        self.setWindowIcon(icon)
        self.setWhatsThis("")
        self.setAutoFillBackground(False)

        self.centralwidget = QWidget(self)
        self.centralwidget.setObjectName("centralwidget")

        self.__initTitle()
        self.__initPPTXGroupBox()
        self.__initCSVGroupBox()
        self.__initSaveGroupBox()
        self.__initConfigGroupBox()
        self.__initStartButton()
        self.__initIcons()

        self.setCentralWidget(self.centralwidget)
        self.statusbar = QStatusBar(self)
        self.statusbar.setObjectName("statusbar")
        self.setStatusBar(self.statusbar)

        QtCore.QMetaObject.connectSlotsByName(self)

    def __initTitle(self):
        """
        Thiết lập tiêu đề.
        """
        self.title = QLabel(self.centralwidget)
        self.title.setEnabled(True)
        self.title.setGeometry(QtCore.QRect(190, 20, 611, 81))
        font = QtGui.QFont()
        font.setFamily("Segoe UI")
        font.setPointSize(26)
        font.setBold(True)
        font.setWeight(75)
        self.title.setFont(font)
        self.title.setMouseTracking(False)
        self.title.setTextFormat(QtCore.Qt.AutoText)
        self.title.setScaledContents(False)
        self.title.setWordWrap(False)
        self.title.setObjectName("title")

    def __initPPTXGroupBox(self):
        """
        Thiết lập nhóm PPTX.
        """
        self.pptx = QGroupBox(self.centralwidget)
        self.pptx.setGeometry(QtCore.QRect(20, 120, 411, 161))
        self.pptx.setObjectName("pptx")

        self.pptx_browse = QPushButton(self.pptx)
        self.pptx_browse.setGeometry(QtCore.QRect(300, 90, 93, 28))
        self.pptx_browse.setObjectName("pptx_browse")

        self.pptx_path = QLineEdit(self.pptx)
        self.pptx_path.setGeometry(QtCore.QRect(20, 90, 271, 31))
        self.pptx_path.setCursorPosition(0)
        self.pptx_path.setDragEnabled(False)
        self.pptx_path.setObjectName("pptx_path")
        self.pptx_path.setReadOnly(True)
        self.pptx_path.setStyleSheet("background-color: #f0f0f0")

        self.pptx_label = QLabel(self.pptx)
        self.pptx_label.setGeometry(QtCore.QRect(20, 40, 371, 31))
        self.pptx_label.setObjectName("pptx_label")

        self.pptx_loaded = QLabel(self.pptx)
        self.pptx_loaded.setGeometry(QtCore.QRect(20, 130, 371, 20))
        self.pptx_loaded.setObjectName("pptx_loaded")
        self.pptx_loaded.setVisible(False)

    def __initCSVGroupBox(self):
        """
        Thiết lập nhóm CSV.
        """
        self.csv = QGroupBox(self.centralwidget)
        self.csv.setGeometry(QtCore.QRect(510, 120, 411, 161))
        self.csv.setObjectName("csv")

        self.csv_broswe = QPushButton(self.csv)
        self.csv_broswe.setGeometry(QtCore.QRect(300, 90, 93, 28))
        self.csv_broswe.setObjectName("csv_broswe")

        self.csv_path = QLineEdit(self.csv)
        self.csv_path.setGeometry(QtCore.QRect(20, 90, 271, 31))
        self.csv_path.setCursorPosition(0)
        self.csv_path.setDragEnabled(False)
        self.csv_path.setObjectName("csv_path")
        self.csv_path.setReadOnly(True)
        self.csv_path.setStyleSheet("background-color: #f0f0f0")

        self.csv_label = QLabel(self.csv)
        self.csv_label.setGeometry(QtCore.QRect(20, 40, 371, 31))
        self.csv_label.setObjectName("csv_label")

        self.csv_loaded = QLabel(self.csv)
        self.csv_loaded.setGeometry(QtCore.QRect(20, 130, 271, 20))
        self.csv_loaded.setObjectName("csv_loaded")
        self.csv_loaded.setVisible(False)

    def __initSaveGroupBox(self):
        """
        Thiết lập nhóm Save.
        """
        self.save = QGroupBox(self.centralwidget)
        self.save.setGeometry(QtCore.QRect(20, 720, 411, 121))
        self.save.setObjectName("save")

        self.save_broswe = QPushButton(self.save)
        self.save_broswe.setGeometry(QtCore.QRect(300, 60, 93, 28))
        self.save_broswe.setObjectName("save_broswe")

        self.save_path = QLineEdit(self.save)
        self.save_path.setGeometry(QtCore.QRect(20, 60, 271, 31))
        self.save_path.setCursorPosition(0)
        self.save_path.setDragEnabled(False)
        self.save_path.setObjectName("save_path")
        self.save_path.setReadOnly(True)
        self.save_path.setStyleSheet("background-color: #f0f0f0")

        self.save_label = QLabel(self.save)
        self.save_label.setGeometry(QtCore.QRect(20, 20, 371, 31))
        self.save_label.setObjectName("save_label")

    def __initConfigGroupBox(self):
        """
        Thiết lập nhóm Config.
        """
        self.config = QGroupBox(self.centralwidget)
        self.config.setGeometry(QtCore.QRect(20, 300, 901, 401))
        self.config.setObjectName("config")

        self.config_text_list = QListWidget(self.config)
        self.config_text_list.setGeometry(QtCore.QRect(20, 60, 401, 301))
        self.config_text_list.setObjectName("config_text_list")
        self.config_text_list.setEnabled(False)

        self.config_image_table = QTableWidget(self.config)
        self.config_image_table.setGeometry(QtCore.QRect(470, 60, 401, 301))
        self.config_image_table.setObjectName("config_image_table")
        self.config_image_table.setColumnCount(2)
        self.config_image_table.setRowCount(0)
        self.config_image_table.setEnabled(False)

        item = QTableWidgetItem()
        font = QtGui.QFont()
        font.setBold(True)
        font.setWeight(75)
        item.setFont(font)
        self.config_image_table.setHorizontalHeaderItem(0, item)

        item = QTableWidgetItem()
        font.setBold(True)
        font.setWeight(75)
        item.setFont(font)
        self.config_image_table.setHorizontalHeaderItem(1, item)

        self.config_image_table.horizontalHeader().setHighlightSections(True)
        self.config_image_table.horizontalHeader().setSortIndicatorShown(False)
        self.config_image_table.horizontalHeader().setStretchLastSection(True)
        self.config_image_table.verticalHeader().setVisible(True)
        self.config_image_table.verticalHeader().setSectionResizeMode(
            QHeaderView.ResizeToContents
        )
        self.config_image_table.horizontalHeader().setSectionResizeMode(
            0, QHeaderView.Stretch
        )

        self.config_text_label = QLabel(self.config)
        self.config_text_label.setGeometry(QtCore.QRect(20, 30, 401, 20))
        self.config_text_label.setObjectName("config_text_label")

        self.config_image_label = QLabel(self.config)
        self.config_image_label.setGeometry(QtCore.QRect(470, 30, 401, 20))
        self.config_image_label.setObjectName("config_image_label")

        self.config_image_viewShapes = QPushButton(self.config)
        self.config_image_viewShapes.setGeometry(QtCore.QRect(770, 25, 93, 28))
        self.config_image_viewShapes.setObjectName("config_image_viewShapes")
        self.config_image_viewShapes.setEnabled(False)

        self.config_image_autodownload_label = QLabel(self.config)
        self.config_image_autodownload_label.setGeometry(
            QtCore.QRect(470, 370, 321, 16)
        )
        self.config_image_autodownload_label.setObjectName(
            "config_image_autodownload_label"
        )

        self.config_image_add_button = QPushButton(self.config)
        self.config_image_add_button.setGeometry(QtCore.QRect(800, 370, 31, 21))
        icon1 = QtGui.QIcon()
        icon1.addPixmap(
            QtGui.QPixmap(ADD_ICON_PATH), QtGui.QIcon.Normal, QtGui.QIcon.Off
        )
        self.config_image_add_button.setIcon(icon1)
        self.config_image_add_button.setObjectName("config_image_add_button")
        self.config_image_add_button.setEnabled(False)

        self.config_image_remove_button = QPushButton(self.config)
        self.config_image_remove_button.setGeometry(QtCore.QRect(840, 370, 31, 21))
        icon2 = QtGui.QIcon()
        icon2.addPixmap(
            QtGui.QPixmap(REMOVE_ICON_PATH), QtGui.QIcon.Normal, QtGui.QIcon.Off
        )
        self.config_image_remove_button.setIcon(icon2)
        self.config_image_remove_button.setObjectName("config_image_remove_button")
        self.config_image_remove_button.setEnabled(False)

        self.config_text_remove_button = QPushButton(self.config)
        self.config_text_remove_button.setGeometry(QtCore.QRect(390, 370, 31, 21))
        self.config_text_remove_button.setIcon(icon2)
        self.config_text_remove_button.setObjectName("config_text_remove_button")
        self.config_text_remove_button.setEnabled(False)

        self.config_text_add_button = QPushButton(self.config)
        self.config_text_add_button.setGeometry(QtCore.QRect(350, 370, 31, 21))
        self.config_text_add_button.setIcon(icon1)
        self.config_text_add_button.setObjectName("config_text_add_button")
        self.config_text_add_button.setEnabled(False)

    def __initStartButton(self):
        """
        Thiết lập nút Start.
        """
        self.start_button = QPushButton(self.centralwidget)
        self.start_button.setGeometry(QtCore.QRect(690, 740, 191, 71))
        self.start_button.setObjectName("start_button")
        self.start_button.setEnabled(False)

    def __initIcons(self):
        """
        Thiết lập các biểu tượng.
        """
        self.logo = QLabel(self.centralwidget)
        self.logo.setGeometry(QtCore.QRect(30, 20, 81, 81))
        self.logo.setObjectName("logo")

        self.github = QLabel(self.centralwidget)
        self.github.setGeometry(QtCore.QRect(870, 10, 31, 31))
        self.github.setObjectName("github")
        self.github.setOpenExternalLinks(True)

        self.about = QLabel(self.centralwidget)
        self.about.setGeometry(QtCore.QRect(930, 10, 31, 31))
        self.about.setObjectName("about")

        self.guide = QLabel(self.centralwidget)
        self.guide.setGeometry(QtCore.QRect(900, 10, 21, 31))
        self.guide.setObjectName("guide")

    def _retranslateUi(self):
        """
        Dịch UI.
        """
        _translate = QtCore.QCoreApplication.translate

        # Tiêu đề cửa sổ
        self.setWindowTitle(_translate("menu", _get_retranslate_window("title")))

        # Tiêu đề
        self.title.setText(_translate("menu", _get_retranslate_window("title").upper()))

        # Logo
        self.logo.setText(
            _translate(
                "menu",
                f'<html><head/><body><p><img src="{LOGO_PATH}"></p></body></html>',
            )
        )
        self.logo.setToolTip(_get_retranslate_window("logo"))

        # GitHub
        self.github.setText(
            _translate(
                "menu",
                f'<html><head/><body><p><a href="{GITHUB_URL}"><img src="{GITHUB_ICON_PATH}"/></a></p></body></html>',
            )
        )
        self.github.setToolTip(_get_retranslate_window("github"))

        # About
        self.about.setText(
            _translate(
                "menu",
                f'<html><head/><body><p><img src="{ABOUT_ICON_PATH}"/></p></body></html>',
            )
        )
        self.about.setToolTip(_get_retranslate_window("about"))

        # Guide
        self.guide.setText(
            _translate(
                "menu",
                f'<html><head/><body><p><img src="{GUIDE_ICON_PATH}"/></p></body></html>',
            )
        )
        self.guide.setToolTip(_get_retranslate_window("guide"))

        # Nhóm PPTX
        self.pptx.setTitle(_translate("menu", _get_retranslate_window("pptx", "title")))
        self.pptx_browse.setText(_translate("menu", _get_retranslate_window("broswe")))
        self.pptx_label.setText(
            _translate(
                "menu",
                f'<html><head/><body><p><span style=" font-weight:600;">{_get_retranslate_window("pptx", "label")} </span>{_get_retranslate_window("pptx", "extension")}</p></body></html>',
            )
        )

        # Nhóm CSV
        self.csv.setTitle(_translate("menu", _get_retranslate_window("csv", "title")))
        self.csv_broswe.setText(_translate("menu", _get_retranslate_window("broswe")))
        self.csv_label.setText(
            _translate(
                "menu",
                f'<html><head/><body><p><span style=" font-weight:600;">{_get_retranslate_window("csv", "label")} </span>{_get_retranslate_window("csv", "extension")}</p></body></html>',
            )
        )

        # Nhóm Config
        self.config.setTitle(
            _translate("menu", _get_retranslate_window("config", "title"))
        )
        # - Config Text
        self.config_text_label.setText(
            _translate(
                "menu",
                f'<html><head/><body><p align="center"><span style=" font-weight:600;">{_get_retranslate_window("config", "text", "label")}</span></p></body></html>',
            )
        )
        self.config_text_list.setSortingEnabled(False)
        # - Config Image
        self.config_image_label.setText(
            _translate(
                "menu",
                f'<html><head/><body><p align="center"><span style=" font-weight:600;">{_get_retranslate_window("config", "image", "label")}</span></p></body></html>',
            )
        )
        item = self.config_image_table.horizontalHeaderItem(0)
        item.setText(
            _translate(
                "menu", _get_retranslate_window("config", "image", "shape_index")
            )
        )
        item = self.config_image_table.horizontalHeaderItem(1)
        item.setText(
            _translate(
                "menu", _get_retranslate_window("config", "image", "placeholder")
            )
        )

        # Nút Xem Shapes
        self.config_image_viewShapes.setText(
            _translate(
                "menu", _get_retranslate_window("config", "image", "view_shapes")
            )
        )

        # Label Tự động tải xuống
        self.config_image_autodownload_label.setText(
            _translate(
                "menu", _get_retranslate_window("config", "image", "auto_download")
            )
        )

        # Nhóm Save
        self.save.setTitle(_translate("menu", _get_retranslate_window("save", "title")))
        self.save_broswe.setText(_translate("menu", _get_retranslate_window("broswe")))
        self.save_label.setText(
            _translate(
                "menu",
                f'<html><head/><body><p><span style=" font-weight:600;">{_get_retranslate_window("save", "label")}</span></p></body></html>',
            )
        )

        # Nút Start
        self.start_button.setText(_translate("menu", _get_retranslate_window("start")))

    def _handleUI(self):
        """
        Xử lý tương tác giao diện người dùng.
        """
        # Mỗi lần save_path, pptx_path, csv_path, config_text_list và config_image_table thay đổi, kiểm tra xem start_button có được enable không
        self.save_path.textChanged.connect(lambda: check_start_button(self))
        self.pptx_path.textChanged.connect(lambda: check_start_button(self))
        self.csv_path.textChanged.connect(lambda: check_start_button(self))

        # Xử lý sự kiện cho các button
        self.config_text_add_button.clicked.connect(
            lambda: {
                config_text.add_item(self.config_text_list),
                check_start_button(self),
            }
        )
        self.config_text_remove_button.clicked.connect(
            lambda: {
                config_text.remove_item(self.config_text_list),
                check_start_button(self),
            }
        )
        self.config_image_add_button.clicked.connect(
            lambda: {
                config_image.add_item(self.config_image_table),
                check_start_button(self),
            }
        )
        self.config_image_remove_button.clicked.connect(
            lambda: {
                config_image.remove_item(self.config_image_table),
                check_start_button(self),
            }
        )
        self.pptx_browse.clicked.connect(lambda: broswe_button.pptx_broswe(self))
        self.csv_broswe.clicked.connect(lambda: broswe_button.csv_broswe(self))
        self.save_broswe.clicked.connect(lambda: broswe_button.save_path_broswe(self))
        self.config_image_viewShapes.clicked.connect(config_image.open_saved_shapes_folder)
        self.start_button.clicked.connect(lambda: start_button.exec(self))
