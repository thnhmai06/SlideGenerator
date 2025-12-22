using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes;

/// <summary>
///     Response containing text placeholders.
/// </summary>
public sealed record SlideScanPlaceholdersSuccess(string FilePath, string[] Placeholders)
    : Response("scanplaceholders");