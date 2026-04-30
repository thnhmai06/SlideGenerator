using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Infrastructure.Sheets.Adapters;

namespace SlideGenerator.Infrastructure.Sheets.Services;

/// <summary>
///     Manages open Syncfusion XlsIO-backed workbooks for workflow execution.
///     Workbooks are always opened read-only; read acquires are shared (concurrent access unrestricted).
/// </summary>
/// <param name="locker">The reader-writer locker used to coordinate access to workbook files.</param>
public sealed class SfWorkbookRegistry(FileLocker locker)
    : FileRegistry<IReadOnlyWorkbook>(locker)
{
    /// <inheritdoc />
    protected override IReadOnlyWorkbook CreateInstance(string normalizedPath, bool isEditable)
    {
        return new SfReadOnlyWorkbook(normalizedPath);
    }
}
