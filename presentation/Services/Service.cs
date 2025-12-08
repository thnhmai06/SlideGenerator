using presentation.Models.Classes.Presentations;
using presentation.Models.Exceptions.Services;

namespace presentation.Services;

public abstract class Service
{
    private readonly Dictionary<string, Presentation> _storage = new();

    protected internal Presentation GetPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        return _storage[filepath] ?? throw new PresentationNotOpenedException(filepath);
    }

    protected abstract Presentation OpenPresentation(string filepath, string? sourcePath);

    public bool AddPresentation(string filepath, string? sourcePath)
    {
        filepath = Path.GetFullPath(filepath);
        if (sourcePath is not null) sourcePath = Path.GetFullPath(sourcePath);

        if (_storage.ContainsKey(filepath)) return false;
        var presentation = OpenPresentation(filepath, sourcePath);
        _storage.Add(filepath, presentation);
        return true;
    }

    public bool RemovePresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        return _storage.Remove(filepath);
    }
}