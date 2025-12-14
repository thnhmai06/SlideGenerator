namespace SlideGenerator.Domain.Slide.Components;

/// <summary>
///     Base configuration for slide content replacement.
/// </summary>
public abstract record SlideConfig(params string[] Columns);