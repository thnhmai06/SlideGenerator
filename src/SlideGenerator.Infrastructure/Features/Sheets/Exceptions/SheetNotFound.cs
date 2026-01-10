namespace SlideGenerator.Infrastructure.Features.Sheets.Exceptions;

/// <summary>
///     Exception thrown when a sheet is not found in a workbook.
/// </summary>
public class SheetNotFound(string sheetName, string? workbookPath = null)
    : KeyNotFoundException(
        $"Table '{sheetName}' not found{(workbookPath != null ? $" in workbook '{workbookPath}'" : "")}.")
{
    public string SheetName { get; } = sheetName;
    public string? WorkbookPath { get; } = workbookPath;
}