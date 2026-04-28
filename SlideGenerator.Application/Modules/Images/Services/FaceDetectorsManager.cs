using System.Collections.Concurrent;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Rules;

namespace SlideGenerator.Application.Modules.Images.Services;

/// <summary>
///     Manages the lifecycle, caching, and selection of face detector models at runtime.
/// </summary>
/// <remarks>Reviewed by @thnhmai06 at 02/03/2026 11:34:31 GMT+7</remarks>
public abstract class FaceDetectorsManager : IFaceDetectorProvider, IAsyncDisposable
{
    private const FaceDetectorModel DefaultModel = FaceDetectorModel.YuNet;
    private readonly ConcurrentDictionary<FaceDetectorModel, FaceDetector> _detectors = new();
    private bool _disposed;

    /// <summary>
    ///     Gets or sets the currently active face detector model key.
    /// </summary>
    public FaceDetectorModel CurrentModel { get; set; } = DefaultModel;

    /// <summary>
    ///     Gets all supported model keys for this manager implementation.
    /// </summary>
    public abstract ICollection<FaceDetectorModel> SupportedDetectors { get; }

    /// <summary>
    ///     Gets all currently initialized model keys in this manager.
    /// </summary>
    public ICollection<FaceDetectorModel> Detectors => _detectors.Keys;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var model in _detectors.Keys)
            await DeInitDetectorAsync(model).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the current detector model instance, ensuring it is initialized before returning.
    /// </summary>
    /// <returns>The fully initialized <see cref="FaceDetector" />.</returns>
    public async Task<FaceDetector> GetDetectorAsync()
    {
        await InitDetectorAsync(CurrentModel).ConfigureAwait(false);
        return _detectors[CurrentModel];
    }

    /// <summary>
    ///     Adds and initializes a detector for the specified model key, if it is not already initialized.
    /// </summary>
    /// <param name="model">The <see cref="FaceDetectorModel" /> key to initialize.</param>
    public async Task InitDetectorAsync(FaceDetectorModel model)
    {
        var detector = _detectors.GetOrAdd(model, CreateDetector);
        if (!detector.IsModelAvailable)
            await detector.InitAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     De-initializes and removes a detector by its model key.
    /// </summary>
    /// <param name="model">The <see cref="FaceDetectorModel" /> key to de-initialize.</param>
    public async Task DeInitDetectorAsync(FaceDetectorModel model)
    {
        _detectors.TryRemove(model, out var detector);
        if (detector is not null) await detector.DeInitAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates a new face detector instance for the specified model key.
    /// </summary>
    /// <param name="model">The <see cref="FaceDetectorModel" /> key to create.</param>
    /// <returns>A new <see cref="FaceDetector" /> instance bound to the specified key.</returns>
    protected abstract FaceDetector CreateDetector(FaceDetectorModel model);
}