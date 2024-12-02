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
