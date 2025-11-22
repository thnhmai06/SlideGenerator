using generator.Models.Classes.Presentations;
using presentation.Models.Exceptions.Services;

namespace presentation.Models.Classes.Services;

public sealed class GeneratingService : Service
{
    private GeneratingService() { }
    private static readonly Lazy<GeneratingService> LazyInstance = new(() => new GeneratingService());
    public static GeneratingService Instance => LazyInstance.Value;

    protected override Presentation OpenPresentation(string filepath, string sourcePath = "")
    {
        return sourcePath.Length == 0
            ? throw new NotEnoughArgumentException(filepath)
            : new DerivedPresentation(filepath, sourcePath);
    }

    //TODO: Write Generating Services
}