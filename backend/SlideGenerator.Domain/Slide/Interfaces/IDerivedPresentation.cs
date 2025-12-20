namespace SlideGenerator.Domain.Slide.Interfaces;

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