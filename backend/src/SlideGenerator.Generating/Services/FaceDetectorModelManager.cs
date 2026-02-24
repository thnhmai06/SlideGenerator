using System.Collections.Concurrent;
using SlideGenerator.Framework.Image.Entities.FaceDetection;
using SlideGenerator.Framework.Image.Models.FaceDetection;

namespace SlideGenerator.Generating.Services;

/// <summary>
///     Manages face detector model selection and lifecycle at runtime.
/// </summary>
public sealed class FaceDetectorModelManager : IAsyncDisposable
{
    private bool _disposed;
    private readonly ConcurrentDictionary<FaceDetectorModelKey, FaceDetectorModel> _models = new();

    /// <summary>
    ///     Gets current model key.
    /// </summary>
    public FaceDetectorModelKey CurrentModelKey { get; private set; } = FaceDetectorModelKey.YuNet;

    /// <summary>
    ///     Selects current model key at runtime.
    /// </summary>
    /// <param name="modelKey">Model key to select.</param>
    public void SelectModel(FaceDetectorModelKey modelKey)
    {
        ThrowIfDisposed();
        CurrentModelKey = modelKey;
    }

    /// <summary>
    ///     Gets current model instance and ensures it is initialized.
    /// </summary>
    public async Task<FaceDetectorModel> GetCurrentModelAsync()
    {
        ThrowIfDisposed();
        var model = GetOrAddModel(CurrentModelKey);
        if (model.IsModelAvailable) return model;

        var initialized = await model.InitAsync().ConfigureAwait(false);
        return initialized
            ? model
            : throw new InvalidOperationException($"Model '{CurrentModelKey}' could not be initialized.");
    }

    /// <summary>
    ///     Add and Initializes model by key.
    /// </summary>
    /// <param name="modelKey">Model key to add and initialize.</param>
    public async Task<bool> InitializeAsync(FaceDetectorModelKey modelKey)
    {
        ThrowIfDisposed();
        var model = GetOrAddModel(modelKey);
        return await model.InitAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     De-initializes model by key.
    /// </summary>
    /// <param name="modelKey">Model key to de-initialize.</param>
    /// <returns><see langword="true"/> if the model was successfully de-initialized; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> DeInitializeAsync(FaceDetectorModelKey modelKey)
    {
        ThrowIfDisposed();
        if (!_models.TryGetValue(modelKey, out var model)) return false;
        return await model.DeInitAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a collection of all supported <see cref="FaceDetectorModel"/>.
    /// </summary>
    /// <returns>A collection of <see cref="FaceDetectorModelKey"/> values representing the supported model keys for the <see cref="FaceDetectorModel"/>.</returns>
    public static ICollection<FaceDetectorModelKey> GetSupportedModelKeys()
    {
        return Enum.GetValues<FaceDetectorModelKey>();
    }

    /// <summary>
    /// Gets a collection of keys that identify the models currently added to the face detector.
    /// </summary>
    /// <returns>A collection of <see cref="FaceDetectorModelKey"/> objects representing the keys of the added models.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
    public ICollection<FaceDetectorModelKey> GetAddedModelKeys()
    {
        ThrowIfDisposed();
        return _models.Keys;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;

        foreach (var key in _models.Keys)
            if (_models.TryRemove(key, out var model))
                model.Dispose();

        return ValueTask.CompletedTask;
    }

    private FaceDetectorModel GetOrAddModel(FaceDetectorModelKey modelKey)
    {
        return _models.GetOrAdd(modelKey, AddModel);
    }

    private static FaceDetectorModel AddModel(FaceDetectorModelKey modelKey)
    {
        return modelKey switch
        {
            FaceDetectorModelKey.YuNet => new YuNetModel(),
            _ => throw new NotSupportedException($"Face detector model '{modelKey}' is not supported.")
        };
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
