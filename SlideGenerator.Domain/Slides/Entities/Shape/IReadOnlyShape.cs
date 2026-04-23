using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using SlideGenerator.Domain.Slides.Entities.Slide;

namespace SlideGenerator.Domain.Slides.Entities.Shape;

/// <summary>
///     Represents a read-only view of a shape within a slide.
/// </summary>
public interface IReadOnlyShape
{
    /// <summary>Gets the parent slide containing this shape.</summary>
    IReadOnlySlide Slide { get; }

    /// <summary>Gets the unique ID of the shape.</summary>
    uint Id { get; }

    /// <summary>Gets the name of the shape.</summary>
    string Name { get; }

    /// <summary>Gets the physical bounding box of the shape.</summary>
    RectangleF Bounds { get; }

    /// <summary>Gets the text content of the shape, if any.</summary>
    string? TextContent { get; }

    /// <summary>Gets a value indicating whether this shape is a picture.</summary>
    bool IsPicture { get; }

    /// <summary>Gets a value indicating whether this shape is filled with a BLIP (image).</summary>
    bool HasBlipFill { get; }

    /// <summary>
    ///     Attempts to retrieve the picture data associated with this shape.
    /// </summary>
    /// <param name="image">
    ///     When this method returns, contains the image data as a byte array if successful; otherwise,
    ///     <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if the picture data was successfully retrieved; otherwise, <see langword="false" />.</returns>
    bool TryGetPicture([MaybeNullWhen(false)] out byte[] image);

    /// <summary>
    ///     Attempts to retrieve the BLIP fill image data associated with this shape.
    /// </summary>
    /// <param name="image">
    ///     When this method returns, contains the image data as a byte array if successful; otherwise,
    ///     <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if the BLIP fill data was successfully retrieved; otherwise, <see langword="false" />.</returns>
    bool TryGetBlipFill([MaybeNullWhen(false)] out byte[] image);
}