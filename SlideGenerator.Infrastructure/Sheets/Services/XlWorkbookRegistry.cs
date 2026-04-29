using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Infrastructure.Sheets.Adapters;

namespace SlideGenerator.Infrastructure.Sheets.Services;

/// <summary>
///     Manages open workbooks backed by file system paths.
///     Workbooks are opened read-only; read acquires are shared (concurrent access unrestricted).
/// </summary>
/// <param name="locker">The reader-writer locker used to coordinate access to workbooks based on their paths.</param>
public sealed class XlWorkbookRegistry(FileLocker locker)
    : FileRegistry<IReadOnlyWorkbook>(locker)
{
    /// <inheritdoc />
    protected override IReadOnlyWorkbook CreateInstance(string normalizedPath, bool isEditable)
    {
        return new XlReadOnlyWorkbook(normalizedPath);
    }
}