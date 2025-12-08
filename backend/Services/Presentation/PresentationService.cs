using TaoSlideTotNghiep.Exceptions.Services;

namespace TaoSlideTotNghiep.Services.Presentation;

public abstract class PresentationService : IPresentationService
{
    private readonly Dictionary<string, Models.Presentations.Presentation> _storage = new();

    protected Models.Presentations.Presentation GetPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        return _storage[filepath] ?? throw new PresentationNotOpenedException(filepath);
    }

    protected abstract Models.Presentations.Presentation OpenPresentation(string filepath, string? sourcePath);

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
