using SlideGenerator.Slides.Models;

namespace SlideGenerator.Services.Generating.Models;

public sealed record GeneratingRequest(
    Recipe Recipe, 
    PresentationExtension OutputExtension, string SaveFolder,
    bool DeleteDownloadImage = false, bool DeleteEditImage = true)
{
    public string SaveFolder { get; init; } = string.IsNullOrWhiteSpace(SaveFolder)
        ? throw new ArgumentException("Save folder cannot be null or whitespace.", nameof(SaveFolder))
        : SaveFolder;
}