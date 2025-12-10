using Application.DTOs.Requests;

namespace Application.DTOs.Responses;

#region Success

public abstract record SheetSuccess(string FilePath, SheetRequestType Type) : SuccessResponse(RequestType.Sheet),
    IFilePathBased;

#region Workbook

/// <summary>
/// Response for opening a sheet file.
/// </summary>
public record OpenBookSheetSuccess(string FilePath) : SheetSuccess(FilePath, SheetRequestType.BookOpen);

/// <summary>
/// Response for closing a sheet file.
/// </summary>
public record CloseBookSheetSuccess(string FilePath) : SheetSuccess(FilePath, SheetRequestType.BookClose);

/// <summary>
/// Response containing Sheet information.
/// </summary>
public record GetSheetsSheetSuccess(string FilePath, Dictionary<string, int> Sheets)
    : SheetSuccess(FilePath, SheetRequestType.BookSheets);

#endregion

#region Worksheet

/// <summary>
/// Response containing Sheet headers.
/// </summary>
public record GetHeadersSheetSuccess(string FilePath, string SheetName, List<string?> Headers)
    : SheetSuccess(FilePath, SheetRequestType.SheetHeaders);

/// <summary>
/// Response containing a row of data.
/// </summary>
public record GetRowSheetSuccess(
    string FilePath,
    string SheetName,
    int RowNumber,
    Dictionary<string, object?> RowData) : SheetSuccess(FilePath, SheetRequestType.SheetRow);

#endregion

#endregion

#region Error

public record SheetError : ErrorResponse,
    ISheetDto
{
    public string FilePath { get; init; }

    public SheetError(string filePath, Exception e) : base(RequestType.Image, e)
    {
        FilePath = filePath;
    }
}

#endregion