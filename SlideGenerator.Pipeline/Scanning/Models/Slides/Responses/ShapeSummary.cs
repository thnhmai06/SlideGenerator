using System.Drawing;
using SlideGenerator.Document.Slide.Models;

namespace SlideGenerator.Pipeline.Scanning.Models.Slides.Responses;

public record ShapeSummary(ShapeIdentifier Identifier, RectangleF Bounds);