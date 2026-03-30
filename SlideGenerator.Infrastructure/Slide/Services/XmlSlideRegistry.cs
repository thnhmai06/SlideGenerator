using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Infrastructure.Slide.Adapters;

namespace SlideGenerator.Infrastructure.Slide.Services;

/// <summary>
///     Manages opened XML-based presentations for workflow execution.
/// </summary>
public sealed class XmlSlideRegistry : IRegistry<IPresentation>, IDisposable
{
    private readonly ConcurrentDictionary<string, IPresentation> _presentations =
        new(StringComparer.OrdinalIgnoreCase);

    public IPresentation GetOrOpen(string filePath, bool isEditable = true)
    {
        return _presentations.GetOrAdd(filePath, path => new XmlPresentation(path, isEditable));
    }

    public bool TryGet(string filePath, [MaybeNullWhen(false)] out IPresentation presentation)
    {
        return _presentations.TryGetValue(filePath, out presentation);
    }

    public bool Close(string filePath)
    {
        if (!_presentations.TryRemove(filePath, out var presentation))
            return false;

        if (presentation is IDisposable disposable)
            disposable.Dispose();

        return true;
    }

    public void Dispose()
    {
        foreach (var key in _presentations.Keys.ToList())
            Close(key);
    }
}
