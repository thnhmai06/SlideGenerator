using System.Drawing;
using SlideGenerator.Documents.Slides.Models;

namespace SlideGenerator.Pipelines.Scanning.Models.Slides.Responses;

public record ShapeSummary(ShapeIdentifier Identifier, RectangleF Bounds);