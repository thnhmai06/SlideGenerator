namespace SlideGenerator.Domain.Slide;

/// <summary>
///     Represents a working presentation for slide generation.
/// </summary>
public interface IWorkingPresentation
{
    /// <summary>
    ///     Gets the file path of the presentation.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    ///     Gets the number of slides in the presentation.
    /// </summary>
    int SlideCount { get; }

    /// <summary>
    ///     Saves the presentation.
    /// </summary>
    void Save();
}