def duplicate_slide(prs, slide_index=1) -> bool:  # count from 1 for win32COM
    # Author: @oceantran27
    # Edit: @thnhmai06
    new_slide = prs.Slides(slide_index).Duplicate()
    prs.Save()
    return new_slide
