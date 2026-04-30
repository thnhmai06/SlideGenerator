using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Domain.Slides.Models.Previews;

namespace SlideGenerator.Domain.Slides.Entities.Slide;

/// <summary>
///     Represents a read-only view of a slide within a presentation.
/// </summary>
public interface IReadOnlySlide
{
    /// <summary>Gets the 1-based position of this slide within the presentation.</summary>
    int Index { get; }

    /// <summary>Gets the unique ID of the slide.</summary>
    uint Id { get; }

    /// <summary>Gets the name of the slide, if any.</summary>
    string? Name { get; }

    /// <summary>Gets the parent presentation containing this slide.</summary>
    IReadOnlyPresentation Presentation { get; }

    /// <summary>
    ///     Recursively lists all read-only shapes within the slide.
    /// </summary>
    /// <returns>A collection of <see cref="IReadOnlyShape" /> instances.</returns>
    IEnumerable<IReadOnlyShape> DescendShapes();

    /// <summary>
    ///     Renders this slide to a PNG image and returns it as a <see cref="SlidePreview" />.
    /// </summary>
    /// <param name="skipPreview">When <see langword="true" />, returns an empty preview immediately without any I/O.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<SlidePreview> GetPreview(bool skipPreview = false, CancellationToken ct = default);
}
