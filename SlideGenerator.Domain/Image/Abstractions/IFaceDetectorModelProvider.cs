namespace SlideGenerator.Domain.Image.Abstractions;

/// <summary>
///     Provides access to the current face detector model.
/// </summary>
/// Reviewed by @thnhmai06 at 02/03/2026 11:37:23 GMT+7
public interface IFaceDetectorModelProvider
{
    /// <summary>
    ///     Gets the current face detector model instance and ensures it is initialized.
    /// </summary>
    /// <returns>The initialized face detector model.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the model could not be initialized.</exception>
    Task<FaceDetectorModel> GetCurrentModelAsync();
}