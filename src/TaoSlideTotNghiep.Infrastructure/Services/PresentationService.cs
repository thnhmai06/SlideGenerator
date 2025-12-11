using Microsoft.Extensions.Logging;
using TaoSlideTotNghiep.Application.Contracts;
using TaoSlideTotNghiep.Domain.Interfaces;
using TaoSlideTotNghiep.Infrastructure.Engines.Models;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Services;

namespace TaoSlideTotNghiep.Infrastructure.Services;

/// <summary>
/// Template presentation service implementation.
/// </summary>
public class TemplateService(ILogger<TemplateService> logger) : Service(logger), ITemplateService
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

/// <summary>
/// Generating presentation service implementation.
/// </summary>
public class GeneratingService(ILogger<GeneratingService> logger) : Service(logger), IGeneratingService
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