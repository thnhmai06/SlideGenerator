using SlideGenerator.Application.Resources.Abstractions;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Infrastructure.Sheets.Adapters;

namespace SlideGenerator.Infrastructure.Sheets.Services;

/// <summary>
///     Manages open workbooks backed by file system paths.
///     Workbooks are read-only so concurrent access is unrestricted (max-count = <see cref="int.MaxValue" />).
/// </summary>
public sealed class XlWorkbookRegistry(IAsyncKeyedLocker<string> locker)
    : FileRegistry<IReadOnlyWorkbook>(locker)
{
    /// <inheritdoc />
    protected override IReadOnlyWorkbook OpenResource(string normalizedPath, bool isEditable)
        => new XlReadOnlyWorkbook(normalizedPath);
}
