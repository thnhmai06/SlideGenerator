namespace SlideGenerator.Application.Slide.DTOs.Components;

/// <summary>
/// DTO for shape data in API responses (with Base64 encoded image).
/// </summary>
public record ShapeDto(uint Id, string Name, string ImageBase64);