"""Configuration management for the application"""

import os
import tomli
import tomli_w
from typing import Optional
from pathlib import Path


class Config:
    """Application configuration with support for environment variables and TOML config file"""
    
    # Default configuration values organized by category
    DEFAULT_CONFIG = {
        "server": {
            "host": "127.0.0.1",
            "port": 5000,
            "debug": False,
        },
        "download": {
            "download_dir": "./downloads",
            "max_concurrent_downloads": 5,
            "max_workers_per_download": 4,
            "chunk_size": 1024 * 1024,  # 1MB
            "enable_parallel_chunks": True,
            "min_file_size_for_parallel": 10 * 1024 * 1024,  # 10MB
        },
        "network": {
            "max_retries": 3,
            "initial_retry_delay": 1.0,
            "max_retry_delay": 60.0,
            "retry_backoff_multiplier": 2.0,
            "retry_on_status_codes": [408, 429, 500, 502, 503, 504],
            "request_timeout": 30,
            "connect_timeout": 10,
        },
        "appearance": {
            "language": "vi",
            "theme": "light",
            "enable_animations": True,
            "show_notifications": True,
            "auto_save": True,
        },
        "processing": {
            "default_shape": "rectangle",
        },
    }
    
    def __init__(self, config_file: Optional[str] = None):
        """
        Initialize configuration
        
        Args:
            config_file: Path to TOML config file (optional)
        """
        self._config = self._deep_copy_config(self.DEFAULT_CONFIG)
        # Use shared app.config.toml in project root by default
        default_path = os.path.join(os.path.dirname(__file__), "..", "..", "app.config.toml")
        self._config_file = config_file or os.environ.get("CONFIG_FILE", default_path)
        
        # Load from file if exists
        self._load_from_file()
        
        # Override with environment variables
        self._load_from_env()
    
    def _deep_copy_config(self, config: dict) -> dict:
        """Deep copy configuration dictionary"""
        result = {}
        for key, value in config.items():
            if isinstance(value, dict):
                result[key] = self._deep_copy_config(value)
            elif isinstance(value, list):
                result[key] = value.copy()
            else:
                result[key] = value
        return result
    
    def _load_from_file(self):
        """Load configuration from TOML file"""
        config_path = Path(self._config_file)
        if config_path.exists():
            try:
                with open(config_path, 'rb') as f:
                    file_config = tomli.load(f)
                    # Update each section
                    for section, values in file_config.items():
                        if section in self._config:
                            self._config[section].update(values)
            except Exception as e:
                print(f"Warning: Failed to load config file {self._config_file}: {e}")
    
    def _load_from_env(self):
        """Load configuration from environment variables"""
        env_mappings = {
            "APP_HOST": ("server", "host"),
            "APP_PORT": ("server", "port"),
            "APP_DEBUG": ("server", "debug"),
            "DOWNLOAD_DIR": ("download", "download_dir"),
            "MAX_CONCURRENT_DOWNLOADS": ("download", "max_concurrent_downloads"),
            "MAX_WORKERS_PER_DOWNLOAD": ("download", "max_workers_per_download"),
            "CHUNK_SIZE": ("download", "chunk_size"),
            "ENABLE_PARALLEL_CHUNKS": ("download", "enable_parallel_chunks"),
            "MIN_FILE_SIZE_FOR_PARALLEL": ("download", "min_file_size_for_parallel"),
            "MAX_RETRIES": ("network", "max_retries"),
            "INITIAL_RETRY_DELAY": ("network", "initial_retry_delay"),
            "MAX_RETRY_DELAY": ("network", "max_retry_delay"),
            "RETRY_BACKOFF_MULTIPLIER": ("network", "retry_backoff_multiplier"),
            "REQUEST_TIMEOUT": ("network", "request_timeout"),
            "CONNECT_TIMEOUT": ("network", "connect_timeout"),
        }
        
        for env_key, (section, config_key) in env_mappings.items():
            env_value = os.environ.get(env_key)
            if env_value is not None and section in self._config:
                current_value = self._config[section].get(config_key)
                # Convert to appropriate type
                if isinstance(current_value, bool):
                    self._config[section][config_key] = env_value.lower() in ('true', '1', 'yes')
                elif isinstance(current_value, int):
                    self._config[section][config_key] = int(env_value)
                elif isinstance(current_value, float):
                    self._config[section][config_key] = float(env_value)
                else:
                    self._config[section][config_key] = env_value
    
    def get(self, section: str, key: str, default=None):
        """Get configuration value from a section"""
        return self._config.get(section, {}).get(key, default)
    
    def get_section(self, section: str) -> dict:
        """Get entire configuration section"""
        return self._config.get(section, {}).copy()
    
    def set(self, section: str, key: str, value):
        """Set configuration value in a section"""
        if section in self._config:
            if key in self._config[section]:
                self._config[section][key] = value
            else:
                raise KeyError(f"Unknown configuration key: {section}.{key}")
        else:
            raise KeyError(f"Unknown configuration section: {section}")
    
    def update_section(self, section: str, updates: dict):
        """Update multiple configuration values in a section"""
        if section not in self._config:
            raise KeyError(f"Unknown configuration section: {section}")
        
        for key, value in updates.items():
            if key in self._config[section]:
                self._config[section][key] = value
            else:
                raise KeyError(f"Unknown configuration key: {section}.{key}")
    
    def save_to_file(self, file_path: Optional[str] = None):
        """
        Save current configuration to TOML file
        
        Args:
            file_path: Path to save config (defaults to current config file)
        """
        save_path = file_path or self._config_file
        try:
            with open(save_path, 'wb') as f:
                tomli_w.dump(self._config, f)
            return True
        except Exception as e:
            print(f"Error saving config to {save_path}: {e}")
            return False
    
    def get_all(self) -> dict:
        """Get all configuration values"""
        return self._deep_copy_config(self._config)
    
    def reset_to_defaults(self):
        """Reset configuration to default values"""
        self._config = self._deep_copy_config(self.DEFAULT_CONFIG)
    
    # Property accessors for common settings (backward compatibility)
    @property
    def host(self) -> str:
        return self._config["server"]["host"]
    
    @property
    def port(self) -> int:
        return self._config["server"]["port"]
    
    @property
    def debug(self) -> bool:
        return self._config["server"]["debug"]
    
    @property
    def download_dir(self) -> str:
        return self._config["download"]["download_dir"]
    
    @property
    def max_concurrent_downloads(self) -> int:
        return self._config["download"]["max_concurrent_downloads"]
    
    @property
    def max_workers_per_download(self) -> int:
        return self._config["download"]["max_workers_per_download"]
    
    @property
    def chunk_size(self) -> int:
        return self._config["download"]["chunk_size"]
    
    @property
    def max_retries(self) -> int:
        return self._config["network"]["max_retries"]
    
    @property
    def initial_retry_delay(self) -> float:
        return self._config["network"]["initial_retry_delay"]
    
    @property
    def max_retry_delay(self) -> float:
        return self._config["network"]["max_retry_delay"]
    
    @property
    def retry_backoff_multiplier(self) -> float:
        return self._config["network"]["retry_backoff_multiplier"]
    
    @property
    def retry_on_status_codes(self) -> list:
        return self._config["network"]["retry_on_status_codes"]
    
    @property
    def request_timeout(self) -> int:
        return self._config["network"]["request_timeout"]
    
    @property
    def connect_timeout(self) -> int:
        return self._config["network"]["connect_timeout"]
    
    @property
    def enable_parallel_chunks(self) -> bool:
        return self._config["download"]["enable_parallel_chunks"]
    
    @property
    def min_file_size_for_parallel(self) -> int:
        return self._config["download"]["min_file_size_for_parallel"]


# Global config instance
config = Config()
