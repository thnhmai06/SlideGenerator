using System.Drawing;
using SlideGenerator.Pipelines.Generating.Models.Identifiers;

namespace SlideGenerator.Pipelines.Scanning.Models.Slides.Responses;

public record ShapeSummary(ShapeIdentifier Identifier, RectangleF Bounds);