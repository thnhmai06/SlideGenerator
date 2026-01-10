using SlideGenerator.Domain.Features.Images.Enums;

namespace SlideGenerator.Domain.Features.Jobs.Components;

/// <summary>
///     Configuration for image replacement in slides.
/// </summary>
public record JobImageConfig(uint ShapeId, ImageRoiType RoiType, ImageCropType CropType, params string[] Columns);