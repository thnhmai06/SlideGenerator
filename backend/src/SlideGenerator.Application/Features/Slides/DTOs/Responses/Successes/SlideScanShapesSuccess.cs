using SlideGenerator.Application.Common.Base.DTOs.Responses;
using SlideGenerator.Application.Features.Slides.DTOs.Components;

namespace SlideGenerator.Application.Features.Slides.DTOs.Responses.Successes;

/// <summary>
///     Response containing template shapes.
/// </summary>
public sealed record SlideScanShapesSuccess(string FilePath, ShapeDto[] Shapes)
    : Response("scanshapes");