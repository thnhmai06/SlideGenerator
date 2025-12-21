using SlideGenerator.Domain.Image.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Components;

/// <summary>
///     Image replacement configuration provided by the client.
/// </summary>
public sealed record SlideImageConfig(uint ShapeId, string[] Columns, ImageRoiType? RoiType, ImageCropType? CropType);