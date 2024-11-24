import win32com.client
# import requests
# import urllib.request
# from PIL import Image


def open_presentation(link_presentation):
    # Author: @oceantran27
    # Edit: @thnhmai06
    try:
        ppt_instance = win32com.client.Dispatch("PowerPoint.Application")
        read_only = True
        has_title = False
        window = False
        if ppt_instance:
            prs = ppt_instance.Presentations.open(
                link_presentation, read_only, has_title, window
            )
            return prs, ppt_instance
        return None, None
    except Exception as e:
        print(f"*ERROR - opening presentation: {e}")
        return None, None


def close_presentation(prs, ppt_instance):
    # Author: @oceantran27
    # Edit: @thnhmai06
    try:
        if prs and ppt_instance:
            prs.Close()
            ppt_instance.Quit()
            del ppt_instance
            return True
        return False
    except Exception as e:
        print(f"*ERROR - closing presentation: {e}")
        return False


def duplicate_slide(prs, number_of_copies, slide_index=1):  # count from 1 for win32COM
    # Author: @oceantran27
    # Edit: @thnhmai06

    try:
        if prs:
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


def main():
    # const
    BASE_PATH = "PATH_TO_PROJECT/"
    PRS_PATH = BASE_PATH + "template/template.pptx"
    IMAGES_OUTPUT_PATH = BASE_PATH + "images/template_images"
    DATA_PATH = BASE_PATH + "data/data.xlsx"
    DATA_IMAGE_PATH = BASE_PATH + "images/data_images"

    # init
    data = pd.read_excel(DATA_PATH)
    data_length = data.shape[0]
    prs, ppt_instance = open_presentation(PRS_PATH)
    # process
    if prs:
        is_duplicate_success = duplicate_slide(prs, data_length, 1)
        if is_duplicate_success:
            replace_text_placeholders(prs, data)
        shape_indices = get_image_shape_indices(prs.Slides(1))
        save_images_from_shapes(PRS_PATH, IMAGES_OUTPUT_PATH, shape_indices)
        # download_data_images(DATA_IMAGE_PATH, data)
    # close
    if prs and ppt_instance:
        close_presentation(prs, ppt_instance)


if __name__ == "__main__":
    main()
