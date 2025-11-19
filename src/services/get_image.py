import requests
import base64

def get_image(image_url):

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