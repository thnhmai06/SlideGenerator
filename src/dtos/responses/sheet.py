from src.dtos.requests.sheet import *
from src.dtos.responses.response import SheetResponse


# ? Group (File)

class OpenFileSheetResponse(SheetResponse):
    def __init__(self, request: OpenFileSheetRequest, error: Exception = None):
        super().__init__(request, error)


class CloseFileSheetResponse(SheetResponse):
    def __init__(self, request: CloseFileSheetRequest, error: Exception = None):
        super().__init__(request, error)


class GetTablesSheetResponse(SheetResponse):
    def __init__(self, request: GetTablesSheetRequest, tables: dict[str, int] = None, error: Exception = None):
        super().__init__(request, error)
        self.tables = tables


# ? Table (Sheet)

class GetTableHeadersSheetResponse(SheetResponse):
    def __init__(self, request: GetTableHeadersSheetRequest, headers: list[str] = None, error: Exception = None):
        super().__init__(request, error)
        self.headers = headers


class GetTableRowSheetResponse(SheetResponse):
    def __init__(self, request: GetTableHeadersSheetRequest, row_data: dict = None, error: Exception = None):
        super().__init__(request, error)
        self.row_data = row_data
