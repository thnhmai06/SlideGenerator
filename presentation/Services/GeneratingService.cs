using presentation.Models.Classes.Presentations;
using presentation.Models.Exceptions.Services;

namespace presentation.Services;

public sealed class GeneratingService : Service
{
    private GeneratingService() { }
    private static readonly Lazy<GeneratingService> LazyInstance = new(() => new GeneratingService());
    public static GeneratingService Instance => LazyInstance.Value;

    protected override Presentation OpenPresentation(string filepath, string? sourcePath)
    {
        return sourcePath is null 
            ? throw new NotEnoughArgumentException(filepath) 
            : new DerivedPresentation(filepath, sourcePath);
    }

    //TODO: Write Generating Services
}