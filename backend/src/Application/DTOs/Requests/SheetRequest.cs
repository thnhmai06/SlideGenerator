using System.Text.Json.Serialization;

namespace Application.DTOs.Requests;

#region Enums

/// <summary>
/// Types of sheet requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SheetRequestType
{
    BookOpen,
    BookClose,
    BookSheets,
    SheetHeaders,
    SheetRow
}

#endregion

#region Records

/// <summary>
/// Base sheet request.
/// </summary>
public abstract record SheetRequest(SheetRequestType Type, string FilePath) : Request(RequestType.Sheet),
    IFilePathBased;

/// <summary>
/// Request to get a specific row from a table.
/// </summary>
public record GetTableRowSheetRequest(string FilePath, string TableName, int RowNumber)
    : SheetRequest(SheetRequestType.SheetRow, FilePath);

/// <summary>
/// Request to get table headers.
/// </summary>
public record GetTableHeadersSheetRequest(string FilePath, string SheetName)
    : SheetRequest(SheetRequestType.SheetHeaders, FilePath);

/// <summary>
/// Request to get all tables in a sheet.
/// </summary>
public record GetTablesSheetRequest(string FilePath) : SheetRequest(SheetRequestType.BookSheets, FilePath);

/// <summary>
/// Request to close a sheet file.
/// </summary>
public record CloseFileSheetRequest(string FilePath) : SheetRequest(SheetRequestType.BookClose, FilePath);

/// <summary>
/// Request to open a sheet file.
/// </summary>
public record OpenFileSheetRequest(string FilePath) : SheetRequest(SheetRequestType.BookOpen, FilePath);

#endregion