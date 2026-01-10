using SlideGenerator.Domain.Features.Slides;

namespace SlideGenerator.Application.Features.Slides;

/// <summary>
///     Interface for template presentation service.
/// </summary>
public interface ISlideTemplateManager
{
    /// <summary>
    ///     Adds a template from the specified file path.
    /// </summary>
    /// <param name="filepath">The path to the template file to add. Cannot be null or empty.</param>
    /// <returns><see langword="true" /> if the template was added successfully; otherwise, <see langword="false" />.</returns>
    bool AddTemplate(string filepath);

    /// <summary>
    ///     Removes the template file at the specified path.
    /// </summary>
    /// <param name="filepath">The full path to the template file to remove. Cannot be null or empty.</param>
    /// <returns><see langword="true" /> if the template was removed successfully; otherwise, <see langword="false" />.</returns>
    bool RemoveTemplate(string filepath);

    /// <summary>
    ///     Retrieves a template presentation from the specified file path.
    /// </summary>
    /// <param name="filepath">The path to the template file to load. Cannot be null or empty.</param>
    /// <returns>An object representing the template presentation loaded from the specified file.</returns>
    ITemplatePresentation GetTemplate(string filepath);
}