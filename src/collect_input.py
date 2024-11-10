from typing import TYPE_CHECKING

if TYPE_CHECKING:
    # Anti-circular import
    from ui.menu import Ui

class ClientInput:
    def __init__(self):
        self.pptx_path = ""
        self.csv_path = ""
        self.save_path = ""
        self.config = {
            "text": [],
            "image": []
        }

    def get_input(self, ui: 'Ui') -> 'ClientInput':
        self.pptx_path = ui.pptx_path.text()
        self.csv_path = ui.csv_path.text()
        self.save_path = ui.save_path.text()

        # Get the text configurations
        for index in range(ui.config_text_list.count()):
            self.config["text"].append(ui.config_text_list.item(index).text())

        # Get the image configurations
        for row in range(ui.config_image_table.rowCount()):
            placeholder = ui.config_image_table.item(row, 0).text()
            replace_shape = ui.config_image_table.item(row, 1).text()
            self.config["image"].append({"placeholder": placeholder, "replace_shape": replace_shape})
        return self
