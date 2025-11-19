"""HTTP and download-related utility functions"""

import os
import re
import uuid
from urllib.parse import urlparse, unquote


def get_filename_from_response(response, url: str) -> str:
    """
    Get filename from HTTP response or URL
    
    Args:
        response: HTTP response object
        url: Original URL
        
    Returns:
        str: Extracted or generated filename
    """
    # Try getting from Content-Disposition header
    if "content-disposition" in response.headers:
        disposition = response.headers["content-disposition"]
        matches = re.findall(r'filename="?([^"]+)"?', disposition)
        if matches:
            return matches[0]

    # Get from URL
    path = urlparse(url).path
    filename = unquote(os.path.basename(path))

    # If no extension, add .jpg
    if not os.path.splitext(filename)[1]:
        filename += ".jpg"

    # If no valid filename, generate random
    if not filename or filename == ".jpg":
        filename = f"image_{uuid.uuid4().hex[:8]}.jpg"

    return filename


def supports_resume(response) -> bool:
    """
    Check if server supports resume (Range requests)
    
    Args:
        response: HTTP response object
        
    Returns:
        bool: True if server supports resume
    """
    return response.headers.get("Accept-Ranges") == "bytes"


def get_resume_header(downloaded_size: int) -> dict:
    """
    Get HTTP header for resuming download
    
    Args:
        downloaded_size: Number of bytes already downloaded
        
    Returns:
        dict: Headers for resume request
    """
    return {"Range": f"bytes={downloaded_size}-"}
