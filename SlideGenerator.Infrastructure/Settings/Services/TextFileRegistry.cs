using System.Collections.Concurrent;
using SlideGenerator.Application.Common;
using SlideGenerator.Application.Settings.Abstractions;
using SlideGenerator.Infrastructure.Settings.Adapters;

namespace SlideGenerator.Infrastructure.Settings.Services;

public sealed class TextFileRegistry : IRegistry<ITextFile>
{
    private readonly Lock _syncRoot = new();
    private readonly ConcurrentDictionary<string, ITextFile> _files =
        new(StringComparer.OrdinalIgnoreCase);

    public ITextFile GetOrOpen(string filePath, bool isEditable = true)
    {
        var normalizedPath = Path.GetFullPath(filePath);

        lock (_syncRoot)
        {
            if (_files.TryGetValue(normalizedPath, out var existing))
            {
                if (isEditable && existing is StreamTextFile { IsEditable: false })
                {
                    existing.Dispose();
                    var upgraded = new StreamTextFile(normalizedPath, isEditable: true);
                    _files[normalizedPath] = upgraded;
                    return upgraded;
                }

                return existing;
            }

            var opened = new StreamTextFile(normalizedPath, isEditable);
            _files[normalizedPath] = opened;
            return opened;
        }
    }

    public bool TryGet(string filePath, out ITextFile? textFile)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        return _files.TryGetValue(normalizedPath, out textFile);
    }

    public bool Close(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        if (!_files.TryRemove(normalizedPath, out var textFile))
            return false;

        textFile.Dispose();
        return true;
    }
}