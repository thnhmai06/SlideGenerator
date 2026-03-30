using SlideGenerator.Domain.Slide.Entities;

namespace SlideGenerator.Application.Slide.Abstractions;

/// <summary>
///     Scans and replaces slide contents (text and images).
/// </summary>
public interface ISlideContentOperator
{
    /// <summary>
    ///     Scans slide content and returns placeholders and image shape identifiers.
    /// </summary>
    /// <param name="slide">Target slide to scan.</param>
    /// <returns>Tuple containing detected text placeholders and image shape IDs.</returns>
    (IReadOnlySet<string> Placeholders, IReadOnlySet<uint> ImageShapeIds) ScanTemplateContent(ISlide slide);

    /// <summary>
    ///     Replaces text placeholders on the specified slide.
    /// </summary>
    /// <param name="slide">Target slide.</param>
    /// <param name="replacements">Replacement map keyed by placeholder name.</param>
    /// <returns>Number of text changes made.</returns>
    int ReplaceText(ISlide slide, IReadOnlyDictionary<string, string> replacements);

    /// <summary>
    ///     Replaces image contents on the specified slide by shape identifier.
    /// </summary>
    /// <param name="slide">Target slide.</param>
    /// <param name="assignments">Mapping from shape ID to local image file path.</param>
    /// <returns>Number of shapes successfully replaced.</returns>
    int ReplaceImages(ISlide slide, IReadOnlyDictionary<uint, string> assignments);
}