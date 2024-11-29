def duplicate_slide(prs, number_of_copies=1, slide_index=1) -> bool:  # count from 1 for win32COM
    # Author: @oceantran27
    # Edit: @thnhmai06

    if prs:
        for i in range(number_of_copies):
            prs.Slides(slide_index).Duplicate()
        prs.Save()
        return True
    return False
