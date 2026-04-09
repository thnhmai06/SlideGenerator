using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using SlideGenerator.Domain.Slides.Entities.Slide;

namespace SlideGenerator.Domain.Slides.Entities.Shape;

public interface IReadOnlyShape
{
    IReadOnlySlide Slide { get; }
    uint Id { get; }
    string Name { get; }
    RectangleF Bounds { get; }
    string? TextContent { get; }
    bool IsPicture { get; }
    bool HasBlipFill { get; }
    bool TryGetPicture([MaybeNullWhen(false)] out byte[] image);
    bool TryGetBlipFill([MaybeNullWhen(false)] out byte[] image);
}