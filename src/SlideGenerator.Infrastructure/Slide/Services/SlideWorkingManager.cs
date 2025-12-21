using System.Collections.Concurrent;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Slide;
using SlideGenerator.Domain.Slide;
using SlideGenerator.Infrastructure.Base;
using SlideGenerator.Infrastructure.Slide.Adapters;
using SlideGenerator.Infrastructure.Slide.Exceptions;
using CoreWorkingPresentation = SlideGenerator.Framework.Slide.Models.WorkingPresentation;

namespace SlideGenerator.Infrastructure.Slide.Services;

/// <summary>
///     Working presentation service implementation.
/// </summary>
public class SlideWorkingManager(ILogger<SlideWorkingManager> logger) : Service(logger), ISlideWorkingManager
{
    private readonly ConcurrentDictionary<string, CoreWorkingPresentation> _storage = new();

    public bool AddWorkingPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);
        var isAdded = false;
        _storage.GetOrAdd(filepath, path =>
        {
            isAdded = true;
            return new CoreWorkingPresentation(path);
        });

        if (isAdded)
            Logger.LogInformation("Added working presentation: {FilePath}", filepath);
        return isAdded;
    }

    public bool RemoveWorkingPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        if (_storage.TryRemove(filepath, out var presentation))
        {
            presentation.Dispose();

            Logger.LogInformation("Removed working presentation: {FilePath}", filepath);
            return true;
        }

        return false;
    }

    public IWorkingPresentation GetWorkingPresentation(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        return _storage.TryGetValue(filepath, out var presentation)
            ? new WorkingPresentationAdapter(presentation)
            : throw new PresentationNotOpened(filepath);
    }

    internal SlidePart CopyFirstSlideToLast(string filepath)
    {
        filepath = Path.GetFullPath(filepath);

        if (!_storage.TryGetValue(filepath, out var presentation))
            throw new PresentationNotOpened(filepath);

        var slideIdList = presentation.GetSlideIdList();
        var firstSlideId = slideIdList?.ChildElements.OfType<SlideId>().First();
        var slideRId = firstSlideId?.RelationshipId?.Value
                       ?? throw new InvalidOperationException("No slide relationship ID found");

        var newPosition = presentation.SlideCount + 1;
        var newSlide = presentation.CopySlide(slideRId, newPosition);

        Logger.LogDebug("Copied first slide to position {Position} in {FilePath}", newPosition, filepath);
        return newSlide;
    }
}