import json
import os
import re
from typing import Dict, Any, Optional, List, Callable, Set
from functools import lru_cache
from pystache import render as render_text
from globals import LANG, TRANSLATION_PATH

# Type định nghĩa cho hàm callback khi ngôn ngữ thay đổi
LanguageChangeCallback = Callable[[str], None]

class TranslationManager:
    """
    Quản lý các bản dịch và cung cấp khả năng đa ngôn ngữ cho ứng dụng.
    
    Attributes:
        _current_lang (str): Ngôn ngữ hiện tại đang sử dụng.
        _fallback_lang (str): Ngôn ngữ dự phòng khi không tìm thấy khóa trong ngôn ngữ hiện tại.
        _translations (Dict[str, Dict[str, Any]]): Từ điển chứa các bản dịch đã tải.
        _available_languages (List[str]): Danh sách các ngôn ngữ có sẵn.
        _language_change_callbacks (Set[LanguageChangeCallback]): Tập hợp các hàm callback khi ngôn ngữ thay đổi.
    """
    
    def __init__(self, default_lang: str = LANG, fallback_lang: str = "vi"):
        """
        Khởi tạo TranslationManager với ngôn ngữ mặc định và ngôn ngữ dự phòng.
        
        Args:
            default_lang (str): Mã ngôn ngữ mặc định.
            fallback_lang (str): Mã ngôn ngữ dự phòng khi không tìm thấy khóa trong ngôn ngữ hiện tại.
        """
        self._current_lang = default_lang
        self._fallback_lang = fallback_lang
        self._translations: Dict[str, Dict[str, Any]] = {}
        self._available_languages = self._get_available_languages()
        self._language_change_callbacks: Set[LanguageChangeCallback] = set()
        
        # Tải ngôn ngữ mặc định và ngôn ngữ dự phòng
        self.load_language(default_lang)
        if fallback_lang != default_lang:
            self.load_language(fallback_lang)
    
    def _get_available_languages(self) -> List[str]:
        """
        Lấy danh sách các ngôn ngữ có sẵn dựa trên các file json trong thư mục translations.
        
        Returns:
            List[str]: Danh sách mã ngôn ngữ có sẵn.
        """
        languages = []
        for file in os.listdir(TRANSLATION_PATH):
            if file.endswith('.json'):
                languages.append(file.split('.')[0])
        return languages
    
    @lru_cache(maxsize=8)
    def _load_translation_file(self, lang_code: str) -> Dict[str, Any]:
        """
        Tải file dịch từ đĩa và lưu vào bộ nhớ cache.
        
        Args:
            lang_code (str): Mã ngôn ngữ cần tải.
            
        Returns:
            Dict[str, Any]: Dữ liệu dịch đã tải.
            
        Raises:
            FileNotFoundError: Nếu file dịch không tồn tại.
        """
        file_path = os.path.join(TRANSLATION_PATH, f"{lang_code}.json")
        try:
            with open(file_path, "r", encoding="utf-8") as file:
                return json.load(file)
        except FileNotFoundError:
            raise FileNotFoundError(f"Không tìm thấy file dịch cho ngôn ngữ: {lang_code}")
    
    def load_language(self, lang_code: str) -> bool:
        """
        Tải ngôn ngữ mới và đặt làm ngôn ngữ hiện tại.
        
        Args:
            lang_code (str): Mã ngôn ngữ cần tải.
            
        Returns:
            bool: True nếu tải thành công, False nếu thất bại.
        """
        try:
            if lang_code not in self._translations:
                self._translations[lang_code] = self._load_translation_file(lang_code)
            
            old_lang = self._current_lang
            self._current_lang = lang_code
            
            # Gọi các callback khi ngôn ngữ thay đổi
            if old_lang != lang_code:
                self._notify_language_change(lang_code)
                
            return True
        except Exception:
            return False
    
    def _notify_language_change(self, new_lang: str) -> None:
        """
        Thông báo cho các callback khi ngôn ngữ thay đổi.
        
        Args:
            new_lang (str): Mã ngôn ngữ mới.
        """
        for callback in self._language_change_callbacks:
            try:
                callback(new_lang)
            except Exception:
                # Bỏ qua lỗi từ callback
                pass
    
    def register_language_change_callback(self, callback: LanguageChangeCallback) -> None:
        """
        Đăng ký callback khi ngôn ngữ thay đổi.
        
        Args:
            callback (LanguageChangeCallback): Hàm callback khi ngôn ngữ thay đổi.
        """
        self._language_change_callbacks.add(callback)
    
    def unregister_language_change_callback(self, callback: LanguageChangeCallback) -> None:
        """
        Hủy đăng ký callback khi ngôn ngữ thay đổi.
        
        Args:
            callback (LanguageChangeCallback): Hàm callback cần hủy đăng ký.
        """
        if callback in self._language_change_callbacks:
            self._language_change_callbacks.remove(callback)
    
    def get_text(self, key_path: str, default: Optional[str] = None) -> str:
        """
        Lấy văn bản dịch theo đường dẫn khóa.
        
        Args:
            key_path (str): Đường dẫn khóa, phân tách bằng dấu chấm (ví dụ: "menu.window.title").
            default (Optional[str]): Giá trị mặc định nếu không tìm thấy khóa.
            
        Returns:
            str: Văn bản dịch hoặc giá trị mặc định nếu không tìm thấy.
        """
        # Thử tìm trong ngôn ngữ hiện tại
        result = self._get_text_from_language(self._current_lang, key_path)
        
        # Nếu không tìm thấy và ngôn ngữ hiện tại khác ngôn ngữ dự phòng, thử tìm trong ngôn ngữ dự phòng
        if result is None and self._current_lang != self._fallback_lang:
            result = self._get_text_from_language(self._fallback_lang, key_path)
        
        # Nếu vẫn không tìm thấy, trả về giá trị mặc định hoặc key_path
        if result is None:
            return default or key_path
        
        return result
    
    def _get_text_from_language(self, lang_code: str, key_path: str) -> Optional[str]:
        """
        Lấy văn bản dịch từ một ngôn ngữ cụ thể.
        
        Args:
            lang_code (str): Mã ngôn ngữ.
            key_path (str): Đường dẫn khóa, phân tách bằng dấu chấm.
            
        Returns:
            Optional[str]: Văn bản dịch hoặc None nếu không tìm thấy.
        """
        if lang_code not in self._translations:
            return None
        
        current_dict = self._translations[lang_code]
        keys = key_path.split('.')
        
        for key in keys:
            if isinstance(current_dict, dict) and key in current_dict:
                current_dict = current_dict[key]
            else:
                return None
        
        if isinstance(current_dict, str):
            return current_dict
        else:
            return None
    
    def get_current_language(self) -> str:
        """
        Lấy mã ngôn ngữ hiện tại.
        
        Returns:
            str: Mã ngôn ngữ hiện tại.
        """
        return self._current_lang
    
    def get_fallback_language(self) -> str:
        """
        Lấy mã ngôn ngữ dự phòng.
        
        Returns:
            str: Mã ngôn ngữ dự phòng.
        """
        return self._fallback_lang
    
    def set_fallback_language(self, lang_code: str) -> bool:
        """
        Đặt ngôn ngữ dự phòng mới.
        
        Args:
            lang_code (str): Mã ngôn ngữ dự phòng mới.
            
        Returns:
            bool: True nếu thành công, False nếu thất bại.
        """
        if lang_code not in self._available_languages:
            return False
        
        # Tải ngôn ngữ dự phòng nếu chưa tải
        if lang_code not in self._translations:
            try:
                self._translations[lang_code] = self._load_translation_file(lang_code)
            except Exception:
                return False
        
        self._fallback_lang = lang_code
        return True
    
    def get_available_languages(self) -> List[str]:
        """
        Lấy danh sách các ngôn ngữ có sẵn.
        
        Returns:
            List[str]: Danh sách mã ngôn ngữ có sẵn.
        """
        return self._available_languages
    
    def format_text(self, key_path: str, **kwargs) -> str:
        """
        Lấy và định dạng văn bản dịch với các tham số.
        
        Args:
            key_path (str): Đường dẫn khóa, phân tách bằng dấu chấm.
            **kwargs: Các tham số để định dạng văn bản.
            
        Returns:
            str: Văn bản dịch đã được định dạng.
        """
        # Chuyển đổi các giá trị None thành xâu rỗng
        for key, value in kwargs.items():
            if value is None:
                kwargs[key] = ""
                
        text = self.get_text(key_path)
        try:
            return render_text(text, kwargs)
        except (KeyError, ValueError):
            return text
    
    def interpolate(self, text: str, **kwargs) -> str:
        """
        Nội suy chuỗi với các tham số và các khóa dịch.
        
        Hỗ trợ cú pháp {key:path.to.translation} để nhúng các khóa dịch khác.
        
        Args:
            text (str): Chuỗi cần nội suy.
            **kwargs: Các tham số để nội suy.
            
        Returns:
            str: Chuỗi đã được nội suy.
        """
        # Tìm tất cả các mẫu {key:path.to.translation}
        pattern = r'\{([a-zA-Z0-9_]+):([a-zA-Z0-9_.]+)\}'
        
        def replace_match(match):
            key = match.group(1)
            path = match.group(2)
            
            # Nếu key có trong kwargs, sử dụng giá trị từ kwargs
            if key in kwargs:
                return str(kwargs[key])
            
            # Ngược lại, lấy giá trị từ bản dịch
            return self.get_text(path)
        
        # Thay thế tất cả các mẫu
        result = re.sub(pattern, replace_match, text)
        
        # Định dạng với các tham số còn lại
        try:
            return result.format(**kwargs)
        except (KeyError, ValueError):
            return result

# Khởi tạo đối tượng TranslationManager toàn cục
translation_manager = TranslationManager()

# Hàm tiện ích để truy cập nhanh
def get_text(key_path: str, default: Optional[str] = None) -> str:
    """
    Lấy văn bản dịch theo đường dẫn khóa.
    
    Args:
        key_path (str): Đường dẫn khóa, phân tách bằng dấu chấm.
        default (Optional[str]): Giá trị mặc định nếu không tìm thấy khóa.
        
    Returns:
        str: Văn bản dịch.
    """
    return translation_manager.get_text(key_path, default)

def format_text(key_path: str, **kwargs) -> str:
    """
    Lấy và định dạng văn bản dịch với các tham số.
    
    Args:
        key_path (str): Đường dẫn khóa, phân tách bằng dấu chấm.
        **kwargs: Các tham số để định dạng văn bản.
        
    Returns:
        str: Văn bản dịch đã được định dạng.
    """
    return translation_manager.format_text(key_path, **kwargs)

def interpolate(text: str, **kwargs) -> str:
    """
    Nội suy chuỗi với các tham số và các khóa dịch.
    
    Hỗ trợ cú pháp {key:path.to.translation} để nhúng các khóa dịch khác.
    
    Args:
        text (str): Chuỗi cần nội suy.
        **kwargs: Các tham số để nội suy.
        
    Returns:
        str: Chuỗi đã được nội suy.
    """
    return translation_manager.interpolate(text, **kwargs)

def change_language(lang_code: str) -> bool:
    """
    Thay đổi ngôn ngữ hiện tại.
    
    Args:
        lang_code (str): Mã ngôn ngữ mới.
        
    Returns:
        bool: True nếu thay đổi thành công, False nếu thất bại.
    """
    return translation_manager.load_language(lang_code)

def get_current_language() -> str:
    """
    Lấy mã ngôn ngữ hiện tại.
    
    Returns:
        str: Mã ngôn ngữ hiện tại.
    """
    return translation_manager.get_current_language()

def get_fallback_language() -> str:
    """
    Lấy mã ngôn ngữ dự phòng.
    
    Returns:
        str: Mã ngôn ngữ dự phòng.
    """
    return translation_manager.get_fallback_language()

def set_fallback_language(lang_code: str) -> bool:
    """
    Đặt ngôn ngữ dự phòng mới.
    
    Args:
        lang_code (str): Mã ngôn ngữ dự phòng mới.
        
    Returns:
        bool: True nếu thành công, False nếu thất bại.
    """
    return translation_manager.set_fallback_language(lang_code)

def get_available_languages() -> List[str]:
    """
    Lấy danh sách các ngôn ngữ có sẵn.
    
    Returns:
        List[str]: Danh sách mã ngôn ngữ có sẵn.
    """
    return translation_manager.get_available_languages()

def register_language_change_callback(callback: LanguageChangeCallback) -> None:
    """
    Đăng ký callback khi ngôn ngữ thay đổi.
    
    Args:
        callback (LanguageChangeCallback): Hàm callback khi ngôn ngữ thay đổi.
    """
    translation_manager.register_language_change_callback(callback)

def unregister_language_change_callback(callback: LanguageChangeCallback) -> None:
    """
    Hủy đăng ký callback khi ngôn ngữ thay đổi.
    
    Args:
        callback (LanguageChangeCallback): Hàm callback cần hủy đăng ký.
    """
    translation_manager.unregister_language_change_callback(callback)

# Để tương thích với code cũ
TRANS = translation_manager._translations.get(LANG, {})
