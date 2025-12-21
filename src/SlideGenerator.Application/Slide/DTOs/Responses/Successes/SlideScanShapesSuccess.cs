using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Slide.DTOs.Components;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes;

/// <summary>
///     Response containing template shapes.
/// </summary>
public sealed record SlideScanShapesSuccess(string FilePath, ShapeDto[] Shapes)
    : Response("scanshapes");