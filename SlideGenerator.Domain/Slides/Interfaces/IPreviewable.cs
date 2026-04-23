using SlideGenerator.Domain.Slides.Models.Previews;

namespace SlideGenerator.Domain.Slides.Interfaces;

/// <summary>
///     Defines a contract for objects that can generate a visual preview.
/// </summary>
/// <typeparam name="T">The type of preview generated, which must inherit from <see cref="ObjectPreview" />.</typeparam>
public interface IPreviewable<out T>
    where T : ObjectPreview
{
    /// <summary>
    ///     Generates and returns the preview data for the object.
    /// </summary>
    /// <returns>A preview object of type <typeparamref name="T" />.</returns>
    T GetPreview();
}
