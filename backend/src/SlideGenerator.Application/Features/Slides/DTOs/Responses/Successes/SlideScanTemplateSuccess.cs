using SlideGenerator.Application.Common.Base.DTOs.Responses;
using SlideGenerator.Application.Features.Slides.DTOs.Components;

namespace SlideGenerator.Application.Features.Slides.DTOs.Responses.Successes;

/// <summary>
///     Response containing shapes and text placeholders.
/// </summary>
public sealed record SlideScanTemplateSuccess(string FilePath, ShapeDto[] Shapes, string[] Placeholders)
    : Response("scantemplate");