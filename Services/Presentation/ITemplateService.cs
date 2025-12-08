using TaoSlideTotNghiep.Models.Presentations;

namespace TaoSlideTotNghiep.Services.Presentation;

public interface ITemplateService : IPresentationService
{
    TemplatePresentation GetTemplate(string filepath);
}
