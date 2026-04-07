using System.Drawing;

namespace SlideGenerator.Domain.Slide.Models.Previews;

/// <param name="Id">The unique identifier of the shape in the slide.</param>
public record ShapePreview(uint Id, string Name, RectangleF Bounds, byte[] Image) : ObjectPreview(Name, Image);