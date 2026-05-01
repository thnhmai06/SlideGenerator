using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Infrastructure.Sheets.Adapters;

namespace SlideGenerator.Infrastructure.Sheets.Services;

/// <summary>
///     Manages open Syncfusion XlsIO-backed workbooks for workflow execution.
///     Workbooks are always opened read-only; read acquires are shared (concurrent access unrestricted).
/// </summary>
public sealed class SfWorkbookRegistry : FileRegistry<IReadOnlyWorkbook>
{
    /// <inheritdoc />
    protected override IReadOnlyWorkbook CreateInstance(string normalizedPath, bool isEditable)
    {
        return new SfReadOnlyWorkbook(normalizedPath);
    }
}
