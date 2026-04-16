namespace SlideGenerator.Domain.Images.Abstractions;

/// <summary>
///     Provides access to the current face detector model.
/// </summary>
/// Reviewed by @thnhmai06 at 02/03/2026 11:37:23 GMT+7
public interface IFaceDetectorProvider
{
    /// <summary>
    ///     Gets the current face detector model instance and ensures it is initialized.
    /// </summary>
    /// <returns>The initialized face detector model, or <see langword="null" /> when initialization fails.</returns>
    Task<FaceDetector> GetCurrentDetectorAsync();
}