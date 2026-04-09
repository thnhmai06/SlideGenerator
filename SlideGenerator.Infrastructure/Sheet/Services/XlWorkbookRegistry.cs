using SlideGenerator.Application.Resources;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Infrastructure.Sheet.Adapter;

namespace SlideGenerator.Infrastructure.Sheet.Services;

/// <summary>
///     Manages opened workbooks backed by file system paths.
/// </summary>
public sealed class XlWorkbookRegistry : Registry<IReadOnlyWorkbook>
{
    /// <summary>
    ///     Opens a read-only workbook adapter for the normalized file path.
    /// </summary>
    /// <param name="normalizedPath">The normalized workbook file path.</param>
    /// <param name="isEditable">A value indicating whether the caller requested editable access.</param>
    /// <returns>A new workbook adapter instance.</returns>
    protected override IReadOnlyWorkbook OpenResource(string normalizedPath, bool isEditable)
    {
        return new ReadOnlyWorkbook(normalizedPath);
    }
}