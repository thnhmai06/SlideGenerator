namespace TaoSlideTotNghiep.Domain.Slide.Components;

/// <summary>
/// Configuration for text replacement in slides.
/// </summary>
public record TextConfig(string Pattern, params string[] Columns) : SlideConfig(Columns);