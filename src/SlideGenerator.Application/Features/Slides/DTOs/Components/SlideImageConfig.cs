using SlideGenerator.Domain.Features.Images.Enums;

namespace SlideGenerator.Application.Features.Slides.DTOs.Components;

/// <summary>
///     Image replacement configuration provided by the client.
/// </summary>
public sealed record SlideImageConfig(uint ShapeId, string[] Columns, ImageRoiType? RoiType, ImageCropType? CropType);