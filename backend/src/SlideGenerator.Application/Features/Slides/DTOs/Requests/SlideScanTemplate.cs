namespace SlideGenerator.Application.Features.Slides.DTOs.Requests;

/// <summary>
///     Request to scan shapes and placeholders from a template presentation.
/// </summary>
public sealed record SlideScanTemplate(string FilePath);