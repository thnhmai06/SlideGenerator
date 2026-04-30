using SlideGenerator.Domain.Slides.Entities.Slide;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Domain.Slides.Entities.Presentation;

/// <summary>
///     Represents a mutable presentation document.
/// </summary>
public interface IPresentation : IReadOnlyPresentation
{
    /// <inheritdoc />
    IEnumerable<IReadOnlySlide> IReadOnlyPresentation.EnumerateSlides()
    {
        return EnumerateSlides();
    }

    /// <summary>
    ///     Lists all editable slides within the presentation.
    /// </summary>
    /// <returns>A collection of <see cref="ISlide" /> instances.</returns>
    new IEnumerable<ISlide> EnumerateSlides();

    /// <summary>
    ///     Copies a slide from the specified source index to a target index.
    /// </summary>
    /// <param name="from">The 1-based index of the slide to copy.</param>
    /// <param name="to">The 1-based index where the copied slide will be placed.</param>
    /// <returns>The newly copied <see cref="ISlide" />.</returns>
    ISlide CopySlide(int from, int to);

    /// <summary>
    ///     Removes the slide at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the slide to remove.</param>
    /// <returns><see langword="true" /> if the slide was successfully removed; otherwise, <see langword="false" />.</returns>
    bool RemoveSlide(int index);

    /// <summary>
    ///     Saves the presentation to disk, optionally changing its format extension.
    /// </summary>
    /// <param name="extension">The target file extension. If <see langword="null" />, the original format is kept.</param>
    void Save(PresentationExtension? extension = null);
}