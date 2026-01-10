namespace SlideGenerator.Application.Features.Slides.DTOs.Components;

/// <summary>
///     Text replacement configuration provided by the client.
/// </summary>
public sealed record SlideTextConfig(string Pattern, string[] Columns);