namespace SlideGenerator.Application.Slide.DTOs.Components;

/// <summary>
///     Shape information used for placeholder mapping.
/// </summary>
public sealed record ShapeDto(uint Id, string Name, string Data, string Kind = "Image", bool IsImage = true);