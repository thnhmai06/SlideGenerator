namespace SlideGenerator.Domain.Slide;

/// <summary>
///     Represents a working presentation for slide generation.
/// </summary>
public interface IWorkingPresentation : IDisposable
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

    /// <summary>
    ///     Removes the slide at the specified position.
    /// </summary>
    /// <param name="position">The slide position/index (1-based)</param>
    void RemoveSlide(int position);
}