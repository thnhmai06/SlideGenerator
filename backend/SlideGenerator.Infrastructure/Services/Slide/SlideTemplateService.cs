using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Slide.Contracts;
using SlideGenerator.Domain.Slide.Interfaces;
using SlideGenerator.Infrastructure.Adapters.Slide;
using SlideGenerator.Infrastructure.Exceptions.Slide;
using SlideGenerator.Infrastructure.Services.Base;
using CoreTemplatePresentation = SlideGenerator.Framework.Slide.Models.TemplatePresentation;

namespace SlideGenerator.Infrastructure.Services.Slide;

/// <summary>
///     Template presentation service implementation.
/// </summary>
public class SlideTemplateService(ILogger<SlideTemplateService> logger) : Service(logger), ISlideTemplateService
{
    private readonly Dictionary<string, CoreTemplatePresentation> _storage = new();

    public bool AddTemplate(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        if (_storage.ContainsKey(filepath)) return false;

        var presentation = new CoreTemplatePresentation(filepath);
        _storage.Add(filepath, presentation);

        Logger.LogInformation("Added template: {FilePath}", filepath);
        return true;
    }

    public bool RemoveTemplate(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        if (_storage.TryGetValue(filepath, out var presentation))
        {
            presentation.Dispose();
            _storage.Remove(filepath);
            Logger.LogInformation("Removed template: {FilePath}", filepath);
            return true;
        }

        return false;
    }

    public ITemplatePresentation GetTemplate(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        if (!_storage.TryGetValue(filepath, out var presentation))
            throw new PresentationNotOpenedException(filepath);

        return new TemplatePresentationAdapter(presentation);
    }
}