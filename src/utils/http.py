import base64
import re
import mimetypes
from urllib.parse import urlparse

import requests

FILENAME_PATTERN = re.compile(r'filename="?([^"]+)"?')


def get_file_extension(response: requests.Response) -> str | None:
    """Get file extension from HTTP response
    Args:
        response (Response): The HTTP response
    Returns:
        str | None: The file extension, or None if not found
    """
    # Content-Disposition
    cd = response.headers.get("Content-Disposition")
    if cd:
        match = FILENAME_PATTERN.search(cd)
        if match:
            filename = match.group(1)
            ext = filename.split(".")[-1]
            return ext

    # Content-Type
    content_type = response.headers.get("Content-Type")
    if content_type:
        mime = content_type.split(";")[0]
        ext = mimetypes.guess_extension(mime)
        if ext:
            return ext

    # URL
    path = urlparse(response.url).path
    if '.' in path:
        ext = path.split('.')[-1]
        return ext

    return None

def correct_image_url(image_url: str):
    """
    Corrects image URLs from Google Drive, OneDrive, and Google Photos to direct download links.
    Args:
        image_url (str): The original image URL.
    Returns:
        str: The corrected direct download URL.
    Raises:
        ValueError: If the URL format is unrecognized or cannot be processed.
    """

    ### Google Drive link
    if 'drive.google.com' in image_url:
        image_id = None
        if '/file/d/' in image_url:
            image_id = image_url.split('/file/d/')[1].split('/')[0]
        elif 'id=' in image_url:
            image_id = image_url.split('id=')[1].split('&')[0]
        elif '/folders/' in image_url:
            # folders_id = image_url.split('/folders/')[1].split('?')[0]
            html_text = requests.get(image_url).text
            image_id = html_text.split(r'/file/d/')[1].split('\\')[0]

        if image_id is None:
            raise ValueError("Cannot extract Google Drive image ID")
        url = f"https://drive.google.com/uc?export=download&id={image_id}"


    ### OneDrive link
    elif '1drv.ms' in image_url or 'onedrive.live.com' in image_url:
        share_token = base64.b64encode(image_url.encode()).decode().rstrip('=')
        url = f"https://api.onedrive.com/v1.0/shares/u!{share_token}/root/content"


    ### Google Photos link
    elif 'photos.app.goo.gl' in image_url or 'photos.google.com' in image_url:
        html_text = requests.get(image_url).text
        import re
        pattern = r'https://lh3\.googleusercontent\.com/[^"]*'
        matches = re.findall(pattern, html_text)
        if matches:
            url = matches[0]
        else:
            raise ValueError("Cannot extract Google Photos URL")

    ### Direct link
    else:
        url = image_url

    return url