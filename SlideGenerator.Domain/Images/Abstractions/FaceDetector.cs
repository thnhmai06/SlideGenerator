using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Images.Models;

namespace SlideGenerator.Domain.Images.Abstractions;

/// <summary>
///     A face detector model.
/// </summary>
/// Reviewed by @thnhmai06 at 01/03/2026 01:38:16 GMT+7
public abstract class FaceDetector : IAsyncDisposable
{
    /// <summary>
    ///     Gets a value indicating whether the face detection model is available for use.
    /// </summary>
    public abstract bool IsModelAvailable { get; }

    /// <summary>
    ///     Asynchronously disposes the face detector.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> representing the disposal operation.</returns>
    public abstract ValueTask DisposeAsync();

    /// <summary>
    ///     Initializes the face detection model asynchronously.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if initialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public abstract Task<bool> InitAsync();

    /// <summary>
    ///     Deinitializes the face detection model asynchronously.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if deinitialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public abstract Task<bool> DeInitAsync();

    /// <summary>
    ///     Detects faces in the specified image.
    /// </summary>
    /// <param name="mat">The image to search for faces.</param>
    /// <returns>
    ///     A list of detected <see cref="Face" /> candidates.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the model is not initialized.</exception>
    public abstract Task<IReadOnlyList<Face>> DetectAsync(IImage mat);
}