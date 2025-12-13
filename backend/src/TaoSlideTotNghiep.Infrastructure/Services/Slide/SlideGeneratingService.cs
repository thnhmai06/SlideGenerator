using Microsoft.Extensions.Logging;
using TaoSlideTotNghiep.Application.Slide.Contracts;
using TaoSlideTotNghiep.Domain.Slide.Interfaces;
using TaoSlideTotNghiep.Infrastructure.Engines.Slide.Models;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Slide;
using TaoSlideTotNghiep.Infrastructure.Services.Base;

namespace TaoSlideTotNghiep.Infrastructure.Services.Slide;

/// <summary>
/// Generating presentation service implementation.
/// </summary>
public class SlideGeneratingService(ILogger<SlideGeneratingService> logger) : Service(logger), ISlideGeneratingService
{
    private readonly Dictionary<string, DerivedPresentation> _storage = new();

    public bool AddDerivedPresentation(string filepath, string sourcePath)
    {
        filepath = Path.GetFullPath(filepath);
        sourcePath = Path.GetFullPath(sourcePath);

        if (_storage.ContainsKey(filepath)) return false;

        var presentation = new DerivedPresentation(filepath, sourcePath);
        _storage.Add(filepath, presentation);

        Logger.LogInformation("Added derived presentation: {FilePath} from {SourcePath}", filepath, sourcePath);
        return true;
    }

    public bool RemoveDerivedPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        var removed = _storage.Remove(filepath);

        if (removed)
            Logger.LogInformation("Removed derived presentation: {FilePath}", filepath);

        return removed;
    }

    public IDerivedPresentation GetDerivedPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        return _storage.GetValueOrDefault(filepath)
               ?? throw new PresentationNotOpenedException(filepath);
    }
}