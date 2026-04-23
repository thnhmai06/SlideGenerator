namespace SlideGenerator.Domain.Images.Abstractions;

/// <summary>
///     Provides access to an initialized <see cref="FaceDetector" />.
/// </summary>
/// Reviewed by @thnhmai06 at 02/03/2026 11:37:23 GMT+7
public interface IFaceDetectorProvider
{
    /// <summary>
    ///     Gets an initialized <see cref="FaceDetector" /> instance.
    /// </summary>
    /// <returns>The <see cref="FaceDetector" />, or <see langword="null" /> if initialization fails.</returns>
    Task<FaceDetector> GetDetectorAsync();
}