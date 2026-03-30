using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Infrastructure.Sheet.Adapter;

namespace SlideGenerator.Infrastructure.Sheet.Services;

/// <summary>
///     Manages opened workbooks backed by file system paths.
/// </summary>
public sealed class XlWorkbookRegistry : IRegistry<IReadOnlyWorkbook>, IDisposable
{
    private readonly ConcurrentDictionary<string, IReadOnlyWorkbook> _workbooks =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyWorkbook GetOrOpen(string filePath, bool isEditable = true)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        return _workbooks.GetOrAdd(normalizedPath, path => new ReadOnlyWorkbook(path));
    }

    public bool TryGet(string filePath, [MaybeNullWhen(false)] out IReadOnlyWorkbook workbook)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        return _workbooks.TryGetValue(normalizedPath, out workbook);
    }

    public bool Close(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        if (!_workbooks.TryRemove(normalizedPath, out var workbook))
            return false;

        workbook.Dispose();
        return true;
    }

    public void Dispose()
    {
        foreach (var path in _workbooks.Keys.ToList())
            Close(path);
    }
}