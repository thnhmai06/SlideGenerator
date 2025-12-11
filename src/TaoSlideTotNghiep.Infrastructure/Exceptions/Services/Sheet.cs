namespace TaoSlideTotNghiep.Infrastructure.Exceptions.Services;

/// <summary>
/// Exception thrown when a table/sheet is not found in a workbook.
/// </summary>
public class TableNotFoundException(string tableName, string? workbookPath = null)
    : KeyNotFoundException(
        $"Table '{tableName}' not found{(workbookPath != null ? $" in workbook '{workbookPath}'" : "")}.")
{
    public string TableName { get; } = tableName;
    public string? WorkbookPath { get; } = workbookPath;
}