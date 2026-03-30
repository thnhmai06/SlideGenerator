using SlideGenerator.Domain.Image.Rules;

namespace SlideGenerator.Domain.Image.Abstractions;

/// <summary>
///     Default implementation of face detector model factory.
/// </summary>
/// Reviewed by @thnhmai06 at 02/03/2026 11:34:25 GMT+7
public interface IFaceDetectorModelFactory
{
    /// <summary>
    ///     Creates a face detector model instance for the specified model key.
    /// </summary>
    /// <param name="modelKey">The model key identifying which model to create.</param>
    /// <returns>A new instance of the requested face detector model.</returns>
    /// <exception cref="NotSupportedException">Thrown when the specified model key is not supported.</exception>
    public FaceDetectorModel CreateModel(FaceDetectorModelKey modelKey);

    /// <summary>
    ///     Géts all supported face detector model keys.
    /// </summary>
    /// <returns>A collection of supported face detector model keys.</returns>
    public ICollection<FaceDetectorModelKey> GetSupportedModelKeys();
}