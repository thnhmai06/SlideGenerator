from src.dtos.requests.request import SheetRequest


# ? Group (File)

class OpenFileSheetRequest(SheetRequest):
    def __init__(self, sheet_path: str):
        super().__init__(sheet_path)


class CloseFileSheetRequest(SheetRequest):
    def __init__(self, sheet_path: str):
        super().__init__(sheet_path)


class GetTablesSheetRequest(SheetRequest):
    def __init__(self, sheet_path: str):
        super().__init__(sheet_path)


# ? Table (Sheet)

class GetTableHeadersSheetRequest(SheetRequest):
    def __init__(self, sheet_path: str, table_name: str):
        super().__init__(sheet_path)
        self.table_name = table_name


class GetTableRowSheetRequest(SheetRequest):
    def __init__(self, sheet_path: str, table_name: str, row_num: int):
        super().__init__(sheet_path)
        self.table_name = table_name
        self.row_num = row_num
