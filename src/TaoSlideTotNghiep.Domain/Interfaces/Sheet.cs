namespace TaoSlideTotNghiep.Domain.Interfaces;

public interface IWorkbook : IDisposable
{
    string FilePath { get; }

    /// <summary>
    /// Gets all tables in the workbook.
    /// </summary>
    IReadOnlyDictionary<string, IWorksheet> Sheets { get; }

    /// <summary>
    /// Gets the workbook name.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets table names with their row counts.
    /// </summary>
    Dictionary<string, int> GetSheetsInfo();
}

public interface IWorksheet
{
    string Name { get; }
    IReadOnlyList<string?> Headers { get; }
    int RowCount { get; }

    /// <summary>
    /// Gets a row by its number (1-based, relative to data rows).
    /// </summary>
    Dictionary<string, string?> GetRow(int rowNumber);

    /// <summary>
    /// Gets all rows as a list of dictionaries.
    /// </summary>
    List<Dictionary<string, string?>> GetAllRows();
}