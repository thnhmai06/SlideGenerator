using System.Collections.Concurrent;
using SlideGenerator.Domain.Image.Abstractions;
using SlideGenerator.Domain.Image.Rules;

namespace SlideGenerator.Application.Image.Services;

/// <summary>
///     Manages face detector model selection and lifecycle at runtime.
/// </summary>
/// Reviewed by @thnhmai06 at 02/03/2026 11:34:31 GMT+7
public sealed class FaceDetectorModelManager : IFaceDetectorModelProvider, IAsyncDisposable
{
    private readonly IFaceDetectorModelFactory _modelFactory;
    private readonly ConcurrentDictionary<FaceDetectorModelKey, FaceDetectorModel> _models = new();
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FaceDetectorModelManager" /> class.
    /// </summary>
    /// <param name="modelFactory">Factory for creating face detector models.</param>
    public FaceDetectorModelManager(IFaceDetectorModelFactory modelFactory)
    {
        _modelFactory = modelFactory ?? throw new ArgumentNullException(nameof(modelFactory));
    }

    /// <summary>
    ///     Gets current model key.
    /// </summary>
    public FaceDetectorModelKey CurrentModelKey { get; private set; } = FaceDetectorModelKey.YuNet;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var key in _models.Keys)
            if (_models.TryRemove(key, out var model))
                await model.DisposeAsync();
    }

    /// <summary>
    ///     Gets current model instance and ensures it is initialized.
    /// </summary>
    /// <returns>The initialized face detector model.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the model could not be initialized.</exception>
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
    ///     Selects current model key at runtime.
    /// </summary>
    /// <param name="modelKey">Model key to select.</param>
    public void SelectModel(FaceDetectorModelKey modelKey)
    {
        ThrowIfDisposed();
        CurrentModelKey = modelKey;
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
    /// <returns><see langword="true" /> if the model was successfully de-initialized; otherwise, <see langword="false" />.</returns>
    public async Task<bool> DeInitializeAsync(FaceDetectorModelKey modelKey)
    {
        ThrowIfDisposed();
        if (!_models.TryGetValue(modelKey, out var model)) return false;
        return await model.DeInitAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets a collection of all supported <see cref="FaceDetectorModel" />.
    /// </summary>
    /// <returns>
    ///     A collection of <see cref="FaceDetectorModelKey" /> values representing the supported model keys for the
    ///     <see cref="FaceDetectorModel" />.
    /// </returns>
    public ICollection<FaceDetectorModelKey> GetSupportedModelKeys()
    {
        return _modelFactory.GetSupportedModelKeys();
    }

    /// <summary>
    ///     Gets a collection of keys that identify the models currently added to the face detector.
    /// </summary>
    /// <returns>A collection of <see cref="FaceDetectorModelKey" /> objects representing the keys of the added models.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
    public ICollection<FaceDetectorModelKey> GetAddedModelKeys()
    {
        ThrowIfDisposed();
        return _models.Keys;
    }

    private FaceDetectorModel GetOrAddModel(FaceDetectorModelKey modelKey)
    {
        return _models.GetOrAdd(modelKey, key => _modelFactory.CreateModel(key));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}