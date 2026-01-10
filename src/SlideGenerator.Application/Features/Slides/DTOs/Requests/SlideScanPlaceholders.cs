namespace SlideGenerator.Application.Features.Slides.DTOs.Requests;

/// <summary>
///     Request to scan text placeholders from a template presentation.
/// </summary>
public sealed record SlideScanPlaceholders(string FilePath);