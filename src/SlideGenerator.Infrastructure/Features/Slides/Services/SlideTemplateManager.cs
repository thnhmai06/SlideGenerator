using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Features.Slides;
using SlideGenerator.Domain.Features.Slides;
using SlideGenerator.Infrastructure.Common.Base;
using SlideGenerator.Infrastructure.Features.Slides.Adapters;
using SlideGenerator.Infrastructure.Features.Slides.Exceptions;
using CoreTemplatePresentation = SlideGenerator.Framework.Slide.Models.TemplatePresentation;

namespace SlideGenerator.Infrastructure.Features.Slides.Services;

/// <summary>
///     Template presentation service implementation.
/// </summary>
public class SlideTemplateManager(ILogger<SlideTemplateManager> logger) : Service(logger), ISlideTemplateManager
{
    private readonly ConcurrentDictionary<string, CoreTemplatePresentation> _storage = new();

    public bool AddTemplate(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        var isAdded = false;
        _storage.GetOrAdd(filepath, path =>
        {
            isAdded = true;
            return new CoreTemplatePresentation(path);
        });

        if (isAdded)
            Logger.LogInformation("Added template presentation: {FilePath}", filepath);

        return isAdded;
    }

    public bool RemoveTemplate(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        if (_storage.TryRemove(filepath, out var presentation))
        {
            presentation.Dispose();

            Logger.LogInformation("Removed template presentation: {FilePath}", filepath);
            return true;
        }

        return false;
    }

    public ITemplatePresentation GetTemplate(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        return _storage.TryGetValue(filepath, out var presentation)
            ? new TemplatePresentationAdapter(presentation)
            : throw new PresentationNotOpened(filepath);
    }
}