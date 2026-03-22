using System.Diagnostics.CodeAnalysis;

namespace SlideGenerator.Domain.Sheet.Entities;

public interface IReadOnlyWorkbook : IDisposable
{
    string? Name { get; }
    bool TryGetWorksheet(string name, [MaybeNullWhen(false)] out IReadOnlyWorksheet readOnlyWorksheet);
    IReadOnlyDictionary<string, int> SummarySheets();
}