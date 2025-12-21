using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Slide.DTOs.Components;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes;

/// <summary>
///     Response containing shapes and text placeholders.
/// </summary>
public sealed record SlideScanTemplateSuccess(string FilePath, ShapeDto[] Shapes, string[] Placeholders)
    : Response("scantemplate");
