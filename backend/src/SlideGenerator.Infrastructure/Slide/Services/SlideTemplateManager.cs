using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Slide;
using SlideGenerator.Domain.Slide;
using SlideGenerator.Infrastructure.Base;
using SlideGenerator.Infrastructure.Slide.Adapters;
using SlideGenerator.Infrastructure.Slide.Exceptions;
using CoreTemplatePresentation = SlideGenerator.Framework.Slide.Models.TemplatePresentation;

namespace SlideGenerator.Infrastructure.Slide.Services;

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
            var corePresentation = new CoreTemplatePresentation(path);
            if (corePresentation.SlideCount != 1)
            {
                corePresentation.Dispose();
                throw new NotOnlyOneSlidePresentation(path);
            }

            return corePresentation;
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