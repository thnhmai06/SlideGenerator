using System.Drawing;
using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Scanning.Models.Slides.Responses;

public record ShapeSummary(ShapeIdentifier Identifier, RectangleF Bounds);