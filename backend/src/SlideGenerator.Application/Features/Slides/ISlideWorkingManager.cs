using SlideGenerator.Domain.Features.Slides;

namespace SlideGenerator.Application.Features.Slides;

/// <summary>
///     Interface for generating presentation service.
/// </summary>
public interface ISlideWorkingManager
{
    /// <summary>
    ///     Adds a working presentation by copying content from the specified source path to the given file path.
    /// </summary>
    /// <param name="filepath">The file path where the working presentation will be created. Cannot be null or empty.</param>
    /// <returns>
    ///     <see langword="true" /> if the working presentation was added successfully; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    bool GetOrAddWorkingPresentation(string filepath);

    /// <summary>
    ///     Removes the working presentation file at the specified path.
    /// </summary>
    /// <param name="filepath">The full path to the working presentation file to remove. Cannot be null or empty.</param>
    /// <returns><see langword="true" /> if the file was successfully removed; otherwise, <see langword="false" />.</returns>
    bool RemoveWorkingPresentation(string filepath);

    /// <summary>
    ///     Retrieves a working presentation from the specified file path.
    /// </summary>
    /// <param name="filepath">The path to the file containing the presentation to load. Cannot be null or empty.</param>
    /// <returns>An object representing the working presentation loaded from the specified file.</returns>
    IWorkingPresentation GetWorkingPresentation(string filepath);
}