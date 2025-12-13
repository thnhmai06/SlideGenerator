namespace SlideGenerator.Infrastructure.Exceptions.Slide;

/// <summary>
/// Exception thrown when a presentation is not opened.
/// </summary>
public class PresentationNotOpenedException(string filepath)
    : InvalidOperationException("The presentation at the specified filepath is not open: " + filepath);