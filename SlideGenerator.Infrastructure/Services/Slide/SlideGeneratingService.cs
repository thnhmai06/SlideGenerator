using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Slide.Contracts;
using SlideGenerator.Domain.Slide.Interfaces;
using SlideGenerator.Infrastructure.Adapters.Slide;
using SlideGenerator.Infrastructure.Exceptions.Slide;
using SlideGenerator.Infrastructure.Services.Base;
using CoreDerivedPresentation = SlideGenerator.Framework.Slide.Models.DerivedPresentation;

namespace SlideGenerator.Infrastructure.Services.Slide;

/// <summary>
/// Generating presentation service implementation.
/// </summary>
public class SlideGeneratingService(ILogger<SlideGeneratingService> logger) : Service(logger), ISlideGeneratingService
{
    private readonly Dictionary<string, CoreDerivedPresentation> _storage = new();

    public bool AddDerivedPresentation(string filepath, string sourcePath)
    {
        filepath = Path.GetFullPath(filepath);
        sourcePath = Path.GetFullPath(sourcePath);

        if (_storage.ContainsKey(filepath)) return false;

        var presentation = new CoreDerivedPresentation(filepath, sourcePath);
        _storage.Add(filepath, presentation);

        Logger.LogInformation("Added derived presentation: {FilePath} from {SourcePath}", filepath, sourcePath);
        return true;
    }

    public bool RemoveDerivedPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        
        if (_storage.TryGetValue(filepath, out var presentation))
        {
            presentation.Dispose();
            _storage.Remove(filepath);
            Logger.LogInformation("Removed derived presentation: {FilePath}", filepath);
            return true;
        }

        return false;
    }

    public IDerivedPresentation GetDerivedPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        
        if (!_storage.TryGetValue(filepath, out var presentation))
            throw new PresentationNotOpenedException(filepath);
        
        return new DerivedPresentationAdapter(presentation);
    }
}