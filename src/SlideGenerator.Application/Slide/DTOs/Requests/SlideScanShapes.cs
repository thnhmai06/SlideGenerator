namespace SlideGenerator.Application.Slide.DTOs.Requests;

/// <summary>
///     Request to scan shapes from a template presentation.
/// </summary>
public sealed record SlideScanShapes(string FilePath);