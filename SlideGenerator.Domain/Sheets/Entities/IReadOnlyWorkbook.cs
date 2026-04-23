using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Domain.Sheets.Entities;

/// <summary>
///     Represents a read-only view of a spreadsheet workbook.
/// </summary>
public interface IReadOnlyWorkbook : IDisposable
{
    /// <summary>Gets the unique identifier for this workbook.</summary>
    WorkbookIdentifier Identifier { get; }

    /// <summary>Gets a read-only list of all worksheets within the workbook.</summary>
    IReadOnlyList<IReadOnlyWorksheet> Worksheets { get; }

    /// <summary>Gets the physical file path of the workbook.</summary>
    string FilePath => Identifier.FilePath;

    /// <summary>Gets the name of the workbook without its file extension.</summary>
    string Name => Identifier.Name;

    /// <summary>
    ///     Attempts to retrieve a worksheet by its name.
    /// </summary>
    /// <param name="name">The exact name of the worksheet to find.</param>
    /// <param name="readOnlyWorksheet">When this method returns, contains the worksheet if found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the worksheet was successfully found; otherwise, <see langword="false" />.</returns>
    bool TryGetWorksheet(string name, [MaybeNullWhen(false)] out IReadOnlyWorksheet readOnlyWorksheet);
}
