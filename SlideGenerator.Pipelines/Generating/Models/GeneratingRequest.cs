using SlideGenerator.Documents.Slides.Models;

namespace SlideGenerator.Pipelines.Generating.Models;

/// <summary>
///     Represents the user-provided request to start a slide generation process.
/// </summary>
/// <param name="Recipe">The mapping recipe defining data sources and targets.</param>
/// <param name="OutputType">The desired file extension for the output presentations.</param>
/// <param name="SaveFolder">The root directory where generated presentations will be saved.</param>
/// <param name="DeleteDownloadImage">True to delete raw downloaded images after processing.</param>
/// <param name="DeleteEditImage">True to delete cropped/resized images after they are embedded in slides.</param>
public sealed record GeneratingRequest(
    Recipe Recipe,
    PresentationType OutputType,
    string SaveFolder,
    bool DeleteDownloadImage = false,
    bool DeleteEditImage = true)
{
    /// <summary>
    ///     Gets the validated save folder path.
    /// </summary>
    public string SaveFolder { get; init; } = string.IsNullOrWhiteSpace(SaveFolder)
        ? throw new ArgumentException("Save folder cannot be null or whitespace.", nameof(SaveFolder))
        : SaveFolder;
}