using generator.Models.Classes.Presentations;
using presentation.Models.Exceptions.Services;

namespace presentation.Models.Classes.Services;

public abstract class Service
{
    private readonly Dictionary<string, Presentation> _storage = new();

    protected internal Presentation GetPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        return _storage[filepath] ?? throw new PresentationNotOpenedException(filepath);
    }

    protected abstract Presentation OpenPresentation(string filepath, string sourcePath = "");

    public bool AddPresentation(string filepath, string sourcePath = "")
    {
        filepath = Path.GetFullPath(filepath);
        sourcePath = (sourcePath.Length == 0 ? sourcePath : Path.GetFullPath(sourcePath));
        return _storage.TryAdd(filepath, OpenPresentation(filepath, sourcePath));
    }

    public bool RemovePresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        return _storage.Remove(filepath);
    }
}