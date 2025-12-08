using TaoSlideTotNghiep.Models.Presentations;

namespace TaoSlideTotNghiep.Services.Presentation;

public sealed class TemplateService : PresentationService, ITemplateService
{
    protected override Models.Presentations.Presentation OpenPresentation(string filepath, string? sourcePath)
    {
        return new TemplatePresentation(filepath);
    }

    public TemplatePresentation GetTemplate(string filepath)
    {
        return (TemplatePresentation)GetPresentation(filepath);
    }

    //TODO: Write Template Services
}
