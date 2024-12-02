from pystache import render
from typing import Callable, Type

def replace_text_placeholders(slide, student: dict, add_log: Callable[[str, str, str, str], None], loglevel: Type) -> None:
    for shape in slide.Shapes:
        if shape.HasTextFrame and shape.TextFrame.HasText: # Xác nhận nó là shape text
            text = shape.TextFrame.TextRange.Text
            new_text = render(text, student)
            if (text != new_text):
                add_log(__name__, loglevel.info, "replace_text", f"{text} -> {new_text}")
                shape.TextFrame.TextRange.Text = new_text