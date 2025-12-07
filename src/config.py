"""Configuration management for the application."""

import yaml
import tempfile
from threading import Lock

from pathlib import Path
import copy

# ? Static constants
APP_NAME = "tao-slide-tot-nghiep"
APP_TYPE = "data"
APP_DESCRIPTION = "Data processing application for Tao Slide Tot Nghiep"
APP_DEFAULT_TEMP = Path(tempfile.gettempdir()) / APP_NAME / APP_TYPE

IMAGE_EXTENSIONS: set[str] = {
    # Common
    'jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp', 'ico',
    # pillow-heif
    'heic', 'heif', 'avif',
    # Tifffile + Scipy
    'tif', 'tiff', 'psd', 'tga', 'fits', 'mat', 'npz',
    # Pillow
    'cur', 'dcx', 'dds', 'fli', 'flc', 'fpx', 'gbr', 'gd',
    'icns', 'im', 'imt', 'iptc', 'mcidas', 'mic', 'msp',
    'pcd', 'pcx', 'pixar', 'ppm', 'pgm', 'pbm', 'pnm',
    'sgi', 'spider', 'wal', 'xbm', 'xpm', 'bw', 'rgb', 'rgba'
}
SPREADSHEET_EXTENSIONS = {'xlsx', 'xlsm', 'xltx', 'xltm'}  # openpyxl


# noinspection PyUnresolvedReferences
class Config:
    """Application Configuration (Singleton)"""
    CONFIG_PATH = Path('./data.config.yaml')
    DEFAULT_CONFIG = {
        "server": {
            "host": "127.0.0.1",
            "port": 5000,
            "debug": False,
        },
        "download": {
            "save_folder": "",
            "max_workers": 5,

            "retry": {
                "max_retries": 3,
                "initial_delay": 1.0,
                "max_delay": 10.0,
                "multiplier": 2.0,
                "on_status_codes": [408, 429, 500, 502, 503, 504],
            },

            "timeout": {
                "connect": 10,
                "request": 30,
            }
        },
        "sheet": {
            "max_workers": 5
        }
    }

    _instance = None
    _initialized = False
    _lock = Lock()

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance

    def __init__(self):
        if Config._initialized:
            return

        self._config = copy.deepcopy(self.DEFAULT_CONFIG)
        self.reload()
        Config._initialized = True

    @classmethod
    def instance(cls):
        return cls()

    # ---------------------------------------------------------
    # Core Methods
    # ---------------------------------------------------------

    def _deep_update(self, dest: dict, src: dict):
        for key, value in src.items():
            if isinstance(value, dict) and isinstance(dest.get(key), dict):
                self._deep_update(dest[key], value)
            else:
                dest[key] = value

    def reload(self):
        """Load configuration YAML file."""
        if not self.CONFIG_PATH.exists():
            self.save()
            return

        with Config._lock:
            try:
                with self.CONFIG_PATH.open("r", encoding="utf-8") as f:
                    data = yaml.safe_load(f)
                if data:
                    self._deep_update(self._config, data)
            except Exception:
                pass  # TODO log

    def save(self):
        """Save current config to YAML."""
        with Config._lock:
            self.CONFIG_PATH.parent.mkdir(parents=True, exist_ok=True)
            with self.CONFIG_PATH.open("w", encoding="utf-8") as f:
                yaml.safe_dump(
                    copy.deepcopy(self._config),
                    f,
                    allow_unicode=True
                )

    def reset_to_defaults(self):
        """Reset to default configuration."""
        with Config._lock:
            self._config = copy.deepcopy(self.DEFAULT_CONFIG)
        self.save()

    # ---------------------------------------------------------
    # Properties
    # ---------------------------------------------------------

    # Server
    @property
    def server_host(self) -> str:
        return str(self._config["server"]["host"])

    @server_host.setter
    def server_host(self, value: str) -> None:
        self._config["server"]["host"] = value

    @property
    def server_port(self) -> int:
        return int(self._config["server"]["port"])

    @server_port.setter
    def server_port(self, value: int) -> None:
        self._config["server"]["port"] = value

    @property
    def server_debug(self) -> bool:
        return bool(self._config["server"]["debug"])

    @server_debug.setter
    def server_debug(self, value: bool) -> None:
        self._config["server"]["debug"] = value

    # Download
    @property
    def save_folder(self) -> str:
        if not self._config["download"]["save_folder"]:
            return str(APP_DEFAULT_TEMP)
        return str(self._config["download"]["save_folder"])

    @save_folder.setter
    def save_folder(self, value: str) -> None:
        self._config["download"]["save_folder"] = value

    @property
    def download_max_workers(self) -> int:
        return int(self._config["download"]["max_workers"])

    @download_max_workers.setter
    def download_max_workers(self, value: int) -> None:
        self._config["download"]["max_workers"] = value

    @property
    def download_retry_max_retries(self) -> int:
        return int(self._config["download"]["retry"]["max_retries"])

    @download_retry_max_retries.setter
    def download_retry_max_retries(self, value: int) -> None:
        self._config["download"]["retry"]["max_retries"] = value

    @property
    def download_retry_initial_delay(self) -> float:
        return float(self._config["download"]["retry"]["initial_delay"])

    @download_retry_initial_delay.setter
    def download_retry_initial_delay(self, value: float) -> None:
        self._config["download"]["retry"]["initial_delay"] = value

    @property
    def download_retry_max_delay(self) -> float:
        return float(self._config["download"]["retry"]["max_delay"])

    @download_retry_max_delay.setter
    def download_retry_max_delay(self, value: float) -> None:
        self._config["download"]["retry"]["max_delay"] = value

    @property
    def download_retry_multiplier(self) -> float:
        return float(self._config["download"]["retry"]["multiplier"])

    @download_retry_multiplier.setter
    def download_retry_multiplier(self, value: float) -> None:
        self._config["download"]["retry"]["multiplier"] = value

    @property
    def download_retry_on_status_codes(self) -> list[int]:
        return list(self._config["download"]["retry"]["on_status_codes"])

    @download_retry_on_status_codes.setter
    def download_retry_on_status_codes(self, value: list[int]) -> None:
        self._config["download"]["retry"]["on_status_codes"] = value

    @property
    def download_timeout_connect(self) -> int:
        return int(self._config["download"]["timeout"]["connect"])

    @download_timeout_connect.setter
    def download_timeout_connect(self, value: int) -> None:
        self._config["download"]["timeout"]["connect"] = value

    @property
    def download_timeout_request(self) -> int:
        return int(self._config["download"]["timeout"]["request"])

    @download_timeout_request.setter
    def download_timeout_request(self, value: int) -> None:
        self._config["download"]["timeout"]["request"] = value

    # Sheet
    @property
    def sheet_max_workers(self) -> int:
        return int(self._config["sheet"]["max_workers"])

    @sheet_max_workers.setter
    def sheet_max_workers(self, value: int) -> None:
        self._config["sheet"]["max_workers"] = value
