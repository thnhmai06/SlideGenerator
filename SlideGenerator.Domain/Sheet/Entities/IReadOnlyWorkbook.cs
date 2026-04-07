using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Domain.Sheet.Models;

namespace SlideGenerator.Domain.Sheet.Entities;

public interface IReadOnlyWorkbook : IDisposable
{
    WorkbookIdentifier Identifier { get; }
    IReadOnlyList<IReadOnlyWorksheet> Worksheets { get; }
    string FilePath => Identifier.FilePath;
    string Name => Identifier.Name;
    bool TryGetWorksheet(string name, [MaybeNullWhen(false)] out IReadOnlyWorksheet readOnlyWorksheet);
}