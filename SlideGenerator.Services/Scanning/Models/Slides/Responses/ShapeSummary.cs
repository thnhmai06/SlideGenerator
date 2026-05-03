using System.Drawing;

namespace SlideGenerator.Services.Scanning.Models.Slides.Responses;

public record ShapeSummary(uint Id, string Name, RectangleF Bounds);