from src.dtos.requests.sheet import *
from src.dtos.responses.response import SheetResponse


# ? Group (File)

class OpenFileSheetResponse(SheetResponse):
    def __init__(self, request: OpenFileSheetRequest):
        super().__init__(request)


class CloseFileSheetResponse(SheetResponse):
    def __init__(self, request: CloseFileSheetRequest):
        super().__init__(request)


class GetTablesSheetResponse(SheetResponse):
    def __init__(self, request: GetTablesSheetRequest, tables: dict[str, int] = None):
        super().__init__(request)
        self.tables = tables


# ? Table (Sheet)

class GetTableHeadersSheetResponse(SheetResponse):
    def __init__(self, request: GetTableHeadersSheetRequest, headers: list[str] = None):
        super().__init__(request)
        self.headers = headers


class GetTableRowSheetResponse(SheetResponse):
    def __init__(self, request: GetTableHeadersSheetRequest, row_data: dict = None):
        super().__init__(request)
        self.row_data = row_data
