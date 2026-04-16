using System.Collections.Concurrent;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Rules;

namespace SlideGenerator.Application.Images.Services;

/// <summary>
///     Manages face detector selection and lifecycle at runtime.
/// </summary>
/// Reviewed by @thnhmai06 at 02/03/2026 11:34:31 GMT+7
public abstract class FaceDetectorManager : IFaceDetectorProvider, IAsyncDisposable
{
    private const FaceDetectorModel DefaultModel = FaceDetectorModel.YuNet; 
    private readonly ConcurrentDictionary<FaceDetectorModel, FaceDetector> _detectors = new();
    private bool _disposed;

    /// <summary>
    ///     Gets current model key.
    /// </summary>
    public FaceDetectorModel CurrentModel { get; set; } = DefaultModel;

    /// <summary>
    ///     Gets current model instance and ensures it is initialized.
    /// </summary>
    /// <returns>The initialized face detector model</returns>
    public async Task<FaceDetector> GetCurrentDetectorAsync()
    {
        await InitDetectorAsync(CurrentModel).ConfigureAwait(false);
        return _detectors[CurrentModel];
    }

    /// <summary>
    ///     Add and Initializes detector by model.
    /// </summary>
    /// <param name="model">Model key to add and initialize.</param>
    public async Task InitDetectorAsync(FaceDetectorModel model)
    {
        var detector = _detectors.GetOrAdd(model, CreateDetector);
        if (!detector.IsModelAvailable)
            await detector.InitAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     De-initializes detector by model.
    /// </summary>
    /// <param name="model">Model key to de-initialize.</param>
    /// <returns><see langword="true" /> if the model was successfully de-initialized; otherwise, <see langword="false" />.</returns>
    public async Task DeInitDetectorAsync(FaceDetectorModel model)
    {
        _detectors.TryRemove(model, out var detector);
        if (detector is not null) await detector.DeInitAsync().ConfigureAwait(false);
    }
    
    /// <summary>
    ///     Gets all supported model for this manager implementation.
    /// </summary>
    /// <returns>A collection of supported face detector model keys.</returns>
    public abstract ICollection<FaceDetectorModel> SupportedDetectors { get; }

    /// <summary>
    ///    Gets all initialized model for this manager implementation.
    /// </summary>
    /// <returns>A collection of initialized face detector model keys.</returns>
    public ICollection<FaceDetectorModel> Detectors => _detectors.Keys;

    /// <summary>
    ///     Creates a face detector for the specified model key.
    /// </summary>
    /// <param name="model">Model key to create.</param>
    /// <returns>A detector instance bound to the specified model key.</returns>
    protected abstract FaceDetector CreateDetector(FaceDetectorModel model);
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var model in _detectors.Keys)
            await DeInitDetectorAsync(model).ConfigureAwait(false);
    }
}