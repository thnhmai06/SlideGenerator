using SlideGenerator.Domain.Image.Enums;

namespace SlideGenerator.Domain.Slide.Components;

/// <summary>
/// Configuration for image replacement in slides.
/// </summary>
public record ImageConfig(uint ShapeId, ImageRoiType RoiType, params string[] Columns) : SlideConfig(Columns);