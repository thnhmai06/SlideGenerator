using TaoSlideTotNghiep.Domain.Image.Enums;

namespace TaoSlideTotNghiep.Domain.Slide.Components;

/// <summary>
/// Configuration for image replacement in slides.
/// </summary>
public record ImageConfig(uint ShapeId, ImageRoiType RoiType, params string[] Columns) : SlideConfig(Columns);