import win32com.client
# import requests
# import urllib.request 
# from PIL import Image 

def open_presentation(link_presentation):
    # Author: @oceantran27
    # Edit: @thnhmai06
    try:
        ppt_instance = win32com.client.Dispatch('PowerPoint.Application')
        read_only = True
        has_title = False
        window    = False
        if ppt_instance:
            prs = ppt_instance.Presentations.open(link_presentation, read_only, has_title, window)
            return prs, ppt_instance
        return None, None
    except Exception as e:
        print(f"*ERROR - opening presentation: {e}")
        return None, None
    
def close_presentation(prs, ppt_instance):
    # Author: @oceantran27
    # Edit: @thnhmai06
    try:
        if (prs and ppt_instance):
            prs.Close()
            ppt_instance.Quit()
            del ppt_instance
            return True
        return False
    except Exception as e:
        print(f"*ERROR - closing presentation: {e}")
        return False

def duplicate_slide(prs, number_of_copies, slide_index = 1): # count from 1 for win32COM
    # Author: @oceantran27
    # Edit: @thnhmai06
    
    try:
        if (prs):
            for i in range(number_of_copies):
                prs.Slides(slide_index).Duplicate()
            prs.Save()
            return True
        return False
    except Exception as e:
        print(f"*ERROR - duplicating slide: {e}")
        return False
    
def replace_text_placeholders(prs, data):
    # Author: @oceantran27
    # Edit: @thnhmai06
    
    column_names = data.columns.tolist()
    for slide_index in range(1, prs.Slides.Count):
        row = data.iloc[slide_index - 1]
        for shape in prs.Slides(slide_index).Shapes:
            if shape.HasTextFrame and shape.TextFrame.HasText:
                text = shape.TextFrame.TextRange.Text
                for column_name in column_names:
                    if column_name in text:
                        new_text = text.replace(column_name, str(row[column_name]))
                        shape.TextFrame.TextRange.Text = new_text
    prs.Save()
