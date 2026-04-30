namespace SlideGenerator.Domain.Slides.Models.Previews;

/// <summary>
///     Represents the visual preview image of a slide.
///     Metadata (Id, Name) is promoted to <c>SlideSummary</c> at the scanning layer.
/// </summary>
/// <param name="Index">The 1-based index of the slide within the presentation.</param>
/// <param name="Image">The preview image data as a byte array.</param>
public sealed record SlidePreview(int Index, byte[] Image);
