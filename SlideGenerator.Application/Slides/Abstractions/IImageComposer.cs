using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Application.Slides.Abstractions;

/// <summary>
///     Defines a contract for scanning and replacing image content within a slide shape.
/// </summary>
public interface IImageComposer
{
    /// <summary>
    ///     Scans the provided read-only shape to extract existing image bytes.
    /// </summary>
    /// <param name="shape">The <see cref="IReadOnlyShape" /> to scan.</param>
    /// <returns>A byte array representing the extracted image, or <see langword="null" /> if none is found.</returns>
    byte[]? Scan(IReadOnlyShape shape);

    /// <summary>
    ///     Replaces the image content of the specified shape with a new image stream.
    /// </summary>
    /// <param name="shape">The editable <see cref="IShape" /> to modify.</param>
    /// <param name="imageStream">The <see cref="Stream" /> containing the new image data.</param>
    /// <returns>An integer representing the result or status code of the replacement operation.</returns>
    int Replace(IShape shape, Stream imageStream);
}