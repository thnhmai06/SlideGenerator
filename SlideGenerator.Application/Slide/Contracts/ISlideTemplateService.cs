using SlideGenerator.Domain.Slide.Interfaces;

namespace SlideGenerator.Application.Slide.Contracts;

/// <summary>
///     Interface for template presentation service.
/// </summary>
public interface ISlideTemplateService
{
    bool AddTemplate(string filepath);
    bool RemoveTemplate(string filepath);
    ITemplatePresentation GetTemplate(string filepath);
}