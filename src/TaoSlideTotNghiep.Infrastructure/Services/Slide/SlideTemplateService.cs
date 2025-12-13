using Microsoft.Extensions.Logging;
using TaoSlideTotNghiep.Application.Slide.Contracts;
using TaoSlideTotNghiep.Domain.Slide.Interfaces;
using TaoSlideTotNghiep.Infrastructure.Engines.Slide.Models;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Slide;
using TaoSlideTotNghiep.Infrastructure.Services.Base;

namespace TaoSlideTotNghiep.Infrastructure.Services.Slide;

/// <summary>
/// Template presentation service implementation.
/// </summary>
public class SlideTemplateService(ILogger<SlideTemplateService> logger) : Service(logger), ISlideTemplateService
{
    private readonly Dictionary<string, TemplatePresentation> _storage = new();

    public bool AddTemplate(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        if (_storage.ContainsKey(filepath)) return false;

        var presentation = new TemplatePresentation(filepath);
        _storage.Add(filepath, presentation);

        Logger.LogInformation("Added template: {FilePath}", filepath);
        return true;
    }

    public bool RemoveTemplate(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        var removed = _storage.Remove(filepath);

        if (removed)
            Logger.LogInformation("Removed template: {FilePath}", filepath);

        return removed;
    }

    public ITemplatePresentation GetTemplate(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        return _storage.GetValueOrDefault(filepath)
               ?? throw new PresentationNotOpenedException(filepath);
    }
}