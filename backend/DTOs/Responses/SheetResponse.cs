using TaoSlideTotNghiep.DTOs.Requests;

namespace TaoSlideTotNghiep.DTOs.Responses
{
    /// <summary>
    /// Base sheet response.
    /// </summary>
    public abstract record SheetResponse(string FilePath, SheetRequestType Type) : Response(RequestType.Sheet, true), IFilePathBased;

    #region Workbook
    /// <summary>
    /// Response for opening a sheet file.
    /// </summary>
    public record OpenBookSheetResponse(string FilePath) : SheetResponse(FilePath, SheetRequestType.BookOpen);

    /// <summary>
    /// Response for closing a sheet file.
    /// </summary>
    public record CloseBookSheetResponse(string FilePath) : SheetResponse(FilePath, SheetRequestType.BookClose);

    /// <summary>
    /// Response containing Sheet information.
    /// </summary>
    public record GetSheetsSheetResponse(string FilePath, Dictionary<string, int> Sheets)
        : SheetResponse(FilePath, SheetRequestType.BookSheets);
    #endregion

    #region Worksheet
    /// <summary>
    /// Response containing Sheet headers.
    /// </summary>
    public record GetHeadersSheetResponse(string FilePath, string SheetName, List<string?> Headers)
        : SheetResponse(FilePath, SheetRequestType.SheetHeaders);

    /// <summary>
    /// Response containing a row of data.
    /// </summary>
    public record GetRowSheetResponse(
        string FilePath,
        string SheetName,
        int RowNumber,
        Dictionary<string, object?> RowData) : SheetResponse(FilePath, SheetRequestType.SheetRow);
    #endregion
}