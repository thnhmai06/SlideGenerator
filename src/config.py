"""Configuration management for the application."""

import yaml
import tempfile

from pathlib import Path
import copy

# ? Static constants
APP_NAME = "tao-slide-tot-nghiep"
CATEGORY = "data"
IMAGE_EXTENSIONS: set[str] = {'jpg', 'jpeg', 'jfif', 'jpe', 'png', 'bmp', 'dib', 'gif',
                              'tif', 'tiff', 'ico', 'heif', 'heic', 'avif', 'webp'}  # Pillow supported formats
SPREADSHEET_EXTENSIONS = {'xlsx', 'xlsm', 'xltx', 'xltm'}  # openpyxl supported formats


# ? Configuration
class Config:
    DEFAULT_CONFIG = {
        "server": {
            "host": "127.0.0.1",
            "port": 5000,
            "debug": False,
        },
        "download": {
            "location": str(Path(tempfile.gettempdir()) / APP_NAME / CATEGORY),
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

    def __init__(self, file_path: Path):
        self.file_path = Path(file_path)
        self._config = copy.deepcopy(self.DEFAULT_CONFIG)
        self.reload()

    def _deep_update(self, dest: dict, src: dict):
        """Recursively update dest with values from src."""
        for key, value in src.items():
            if isinstance(value, dict) and isinstance(dest.get(key), dict):
                self._deep_update(dest[key], value)
            else:
                dest[key] = value

    def reload(self):
        """Load configuration from config file, merging with defaults."""
        if not self.file_path.exists():
            try:
                self.save()
            except Exception:
                # TODO: Log error
                pass
            return

        try:
            with self.file_path.open("r", encoding="utf-8") as f:
                data = yaml.safe_load(f)
            if data:
                self._deep_update(self._config, data)
        except Exception:
            # TODO: Log error
            pass

    def save(self):
        """Serialize current config to YAML (convert Paths to strings)."""
        to_write = copy.deepcopy(self._config)

        self.file_path.parent.mkdir(parents=True, exist_ok=True)
        with self.file_path.open("w", encoding="utf-8") as f:
            yaml.safe_dump(to_write, f, allow_unicode=True)

    # Server
    @property
    def server_host(self):
        return self._config["server"]["host"]

    @server_host.setter
    def server_host(self, value):
        self._config["server"]["host"] = value

    @property
    def server_port(self):
        return self._config["server"]["port"]

    @server_port.setter
    def server_port(self, value):
        self._config["server"]["port"] = value

    @property
    def server_debug(self):
        return self._config["server"]["debug"]

    @server_debug.setter
    def server_debug(self, value):
        self._config["server"]["debug"] = value

    # Download
    @property
    def download_location(self):
        return self._config["download"]["location"]

    @download_location.setter
    def download_location(self, value):
        self._config["download"]["location"] = value

    @property
    def download_max_workers(self):
        return self._config["download"]["max_workers"]

    @download_max_workers.setter
    def download_max_workers(self, value):
        self._config["download"]["max_workers"] = value

    @property
    def download_retry_max_retries(self):
        return self._config["download"]["retry"]["max_retries"]

    @download_retry_max_retries.setter
    def download_retry_max_retries(self, value):
        self._config["download"]["retry"]["max_retries"] = value

    @property
    def download_retry_initial_delay(self):
        return self._config["download"]["retry"]["initial_delay"]

    @download_retry_initial_delay.setter
    def download_retry_initial_delay(self, value):
        self._config["download"]["retry"]["initial_delay"] = value

    @property
    def download_retry_max_delay(self):
        return self._config["download"]["retry"]["max_delay"]

    @download_retry_max_delay.setter
    def download_retry_max_delay(self, value):
        self._config["download"]["retry"]["max_delay"] = value

    @property
    def download_retry_multiplier(self):
        return self._config["download"]["retry"]["multiplier"]

    @download_retry_multiplier.setter
    def download_retry_multiplier(self, value):
        self._config["download"]["retry"]["multiplier"] = value

    @property
    def download_retry_on_status_codes(self):
        return self._config["download"]["retry"]["on_status_codes"]

    @download_retry_on_status_codes.setter
    def download_retry_on_status_codes(self, value):
        self._config["download"]["retry"]["on_status_codes"] = value

    @property
    def download_timeout_connect(self):
        return self._config["download"]["timeout"]["connect"]

    @download_timeout_connect.setter
    def download_timeout_connect(self, value):
        self._config["download"]["timeout"]["connect"] = value

    @property
    def download_timeout_request(self):
        return self._config["download"]["timeout"]["request"]

    @download_timeout_request.setter
    def download_timeout_request(self, value):
        self._config["download"]["timeout"]["request"] = value

    # Sheet
    @property
    def sheet_max_workers(self):
        return self._config["sheet"]["max_workers"]

    @sheet_max_workers.setter
    def sheet_max_workers(self, value):
        self._config["sheet"]["max_workers"] = value


CONFIG = Config(Path('./data.config.yaml'))
