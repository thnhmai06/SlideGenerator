using SlideGenerator.Domain.Image.Enums;

namespace SlideGenerator.Domain.Job.Components;

/// <summary>
///     Configuration for image replacement in slides.
/// </summary>
public record JobImageConfig(uint ShapeId, ImageRoiType RoiType, ImageCropType CropType, params string[] Columns);