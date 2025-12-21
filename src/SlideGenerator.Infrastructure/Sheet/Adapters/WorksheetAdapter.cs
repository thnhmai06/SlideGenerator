using SlideGenerator.Domain.Sheet.Interfaces;
using CoreWorksheet = SlideGenerator.Framework.Sheet.Contracts.IWorksheet;

namespace SlideGenerator.Infrastructure.Sheet.Adapters;

/// <summary>
///     Adapter to convert SlideGenerator.Framework.Sheet.Contracts.IWorksheet to Domain.Sheet.Interfaces.ISheet.
/// </summary>
internal sealed class WorksheetAdapter(CoreWorksheet worksheet) : ISheet
{
    public string Name => worksheet.Name;
    public IReadOnlyList<string?> Headers => worksheet.Headers;
    public int RowCount => worksheet.RowCount;

    public Dictionary<string, string?> GetRow(int rowNumber)
    {
        return worksheet.GetRow(rowNumber);
    }

    public List<Dictionary<string, string?>> GetAllRows()
    {
        return worksheet.GetAllRows();
    }
}