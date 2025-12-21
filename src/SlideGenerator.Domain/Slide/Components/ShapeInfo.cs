namespace SlideGenerator.Domain.Slide.Components;

/// <summary>
///     Represents metadata for a slide shape.
/// </summary>
public sealed record ShapeInfo(uint Id, string Name, string Kind, bool IsImage);