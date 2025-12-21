namespace SlideGenerator.Infrastructure.Slide.Exceptions;

/// <summary>
///     Exception thrown when a presentation is not opened.
/// </summary>
public class PresentationNotOpened(string filepath)
    : InvalidOperationException("The presentation at the specified filepath is not open: " + filepath);