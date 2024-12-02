import requests
import os
import re

def get_image_extension_from_url(url) -> str:
    response = requests.head(url, allow_redirects=True)
    content_type = response.headers.get("Content-Type", "")
    extension = str()
    if content_type == "image/jpeg":
        extension = ".jpg"
    elif content_type == "image/png":
        extension = ".png"
    elif content_type == "image/webp":
        extension = ".webp"
    elif content_type == "image/jpeg":
        extension = ".jpeg"
    return extension


class GoogleDrive:
    def is_drive_link(url: str) -> bool:
        IS_LINK_REGEX = re.compile(r"https://drive.google.com/.*")
        return IS_LINK_REGEX.match(url) is not None

    def get_google_drive_file_id(url) -> str | None:
        FILE_ID_REGEX = r"/file/d/([a-zA-Z0-9_-]+)"
        match = re.search(FILE_ID_REGEX, url)
        if match:
            assert isinstance(match, re.Match[bytes])
            return match.group(1)
        else:
            return None

    class _DownloadImageReturn():
        def __init__(self, status: int, file_path: str = None, error: requests.exceptions.RequestException = None):
            """
            Initialize the DownloadImageReturn object.
            Args:
                status (int): The status of the image download. 
                              1 indicates a successful download, 
                              0 indicates a failure, 
                              -1 indicates the file is not an image.
                file_path (str): The file path of the image.
            """
            self.status = status
            self.file_path = file_path
            self.error = error

    def download_image_from_google_drive(url: str, save_path: str, num: int) -> _DownloadImageReturn:
        from gdown import download

        FORMATTED_URL = "https://drive.google.com/uc?id="
        file_id = GoogleDrive.get_google_drive_file_id(url)
        extension = get_image_extension_from_url(url)

        if not extension:
            return GoogleDrive._DownloadImageReturn(status=-1)
        else:
            try:
                url = FORMATTED_URL + file_id
                file_path = os.path.join(save_path, f"replace_image_{num}{extension}")
                download(url, file_path, quiet=False)
                
                return GoogleDrive._DownloadImageReturn(status=1, file_path=file_path)

            except requests.exceptions.RequestException as e:
                return GoogleDrive._DownloadImageReturn(status=0, error=e)
            
class LocalFile:
    def is_image_file(path: str) -> bool:
        return os.path.isfile(path) and path.endswith((".jpg", ".jpeg", ".png", ".webp"))