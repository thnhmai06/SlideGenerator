using SlideGenerator.Domain.Images.Models;

namespace SlideGenerator.Domain.Images.Abstractions;

/// <summary>
///     A face detector model.
/// </summary>
/// Reviewed by @thnhmai06 at 01/03/2026 01:38:16 GMT+7
public abstract class FaceDetectorModel : IAsyncDisposable
{
    /// <summary>
    ///     Gets a value indicating whether the face detection model is available for use.
    /// </summary>
    public abstract bool IsModelAvailable { get; }

    /// <summary>
    ///     Initializes the face detection model asynchronously.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous initialization operation. The task result is <see langword="true" /> if the
    ///     model was successfully initialized; otherwise, <see langword="false" />.
    /// </returns>
    public abstract Task<bool> InitAsync();

    /// <summary>
    ///     Deinitializes the face detection model asynchronously.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous deinitialization operation. The task result is <see langword="true" /> if
    ///     the deinitialization completed successfully; otherwise, <see langword="false" />.
    /// </returns>
    public abstract Task<bool> DeInitAsync();

    /// <summary>
    ///     Attempts to detect faces in the specified image.
    /// </summary>
    /// <param name="mat">The mat in which to search for faces. Must not be null.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is a list of detected
    ///     face candidates. The list is empty if no faces are detected or if detection fails.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the model has not been initialized.</exception>
    public abstract Task<List<FaceInfo>> DetectAsync(IMat mat);

    public abstract ValueTask DisposeAsync();
}