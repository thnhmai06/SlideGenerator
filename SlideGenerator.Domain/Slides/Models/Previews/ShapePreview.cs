using System.Drawing;

namespace SlideGenerator.Domain.Slides.Models.Previews;

/// <summary>
///     Represents a preview of a specific shape.
/// </summary>
/// <param name="Id">The unique identifier of the shape in the slide.</param>
/// <param name="Name">The name of the shape.</param>
/// <param name="Bounds">The physical bounding box of the shape.</param>
/// <param name="Image">The preview image data as a byte array.</param>
public record ShapePreview(uint Id, string Name, RectangleF Bounds, byte[] Image) : ObjectPreview(Name, Image);