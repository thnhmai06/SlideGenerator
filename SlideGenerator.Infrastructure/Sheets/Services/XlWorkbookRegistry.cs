using SlideGenerator.Application.Modules.Resources.Abstractions;
using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Infrastructure.Sheets.Adapters;

namespace SlideGenerator.Infrastructure.Sheets.Services;

/// <summary>
///     Manages open workbooks backed by file system paths.
///     Workbooks are read-only so concurrent access is unrestricted (max-count = <see cref="int.MaxValue" />).
/// </summary>
/// <param name="locker">The locker used to coordinate access to workbooks based on their paths.</param>
public sealed class XlWorkbookRegistry(IAsyncKeyedLocker<string> locker)
    : FileRegistry<IReadOnlyWorkbook>(locker)
{
    /// <inheritdoc />
    protected override IReadOnlyWorkbook OpenResource(string normalizedPath, bool isEditable)
    {
        return new XlReadOnlyWorkbook(normalizedPath);
    }
}