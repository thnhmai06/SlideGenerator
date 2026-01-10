using SlideGenerator.Application.Common.Base.DTOs.Responses;

namespace SlideGenerator.Application.Features.Slides.DTOs.Responses.Successes;

/// <summary>
///     Response containing text placeholders.
/// </summary>
public sealed record SlideScanPlaceholdersSuccess(string FilePath, string[] Placeholders)
    : Response("scanplaceholders");