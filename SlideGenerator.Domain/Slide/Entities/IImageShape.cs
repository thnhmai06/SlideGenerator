using System.Drawing;
using SlideGenerator.Domain.Slide.Rules;

namespace SlideGenerator.Domain.Slide.Entities;

public interface IImageShape : IObject
{
    uint Id { get; }
    string Name { get; }
    ImageShapeType Type { get; }
    RectangleF Bounds { get; }
}