using generator.Models.Classes.Presentations;

namespace presentation.Services;

public sealed class TemplateService : Service
{
    private TemplateService() { }
    private static readonly Lazy<TemplateService> LazyInstance = new(() => new TemplateService());
    public static TemplateService Instance => LazyInstance.Value;

    protected override Presentation OpenPresentation(string filepath, string? sourcePath)
    {
        return new TemplatePresentation(filepath);
    }

    //TODO: Write Template Services
}