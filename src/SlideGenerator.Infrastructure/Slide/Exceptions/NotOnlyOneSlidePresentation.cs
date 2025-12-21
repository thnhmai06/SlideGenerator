namespace SlideGenerator.Infrastructure.Slide.Exceptions;

/// <summary>
///     The exception that is thrown when an operation requires a presentation to contain only one slide, but the specified
///     presentation does not.
/// </summary>
/// <param name="filePath">The path to the presentation file that caused the exception.</param>
public class NotOnlyOneSlidePresentation(string filePath)
    : ArgumentException("Presentation {filePath} is not only one slide Presentation.", filePath)
{
    public string FilePath { get; } = filePath;
}