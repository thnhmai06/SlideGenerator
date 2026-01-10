namespace SlideGenerator.Domain.Features.Jobs.Components;

/// <summary>
///     Configuration for text replacement in slides.
/// </summary>
public record JobTextConfig(string Pattern, params string[] Columns);