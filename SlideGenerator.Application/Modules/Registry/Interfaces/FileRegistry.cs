using AsyncKeyedLock;
using SlideGenerator.Application.Modules.Registry.Entities;

namespace SlideGenerator.Application.Modules.Registry.Interfaces;

/// <summary>
///     Async registry specialized for file-backed resources.
///     Keys are normalized to full paths and compared case-insensitively (ordinal).
/// </summary>
/// <typeparam name="T">The resource type stored by the registry.</typeparam>
public abstract class FileRegistry<T>
{
    /// <summary>
    ///     The locker used to manage asynchronous keyed locks for file paths.
    /// </summary>
    private readonly AsyncKeyedLocker<string> _writeLocker = new();

    /// <summary>
    ///     Creates a fresh resource instance for <paramref name="filePath" /> and acquires the
    ///     appropriate lock (shared for reads, exclusive for writes).
    /// </summary>
    /// <param name="filePath">File path used as the registry key (normalized internally).</param>
    /// <param name="writeable">
    ///     <see langword="true" /> to open the resource for editing and acquire a write lock;
    ///     <see langword="false" /> to open read-only and acquire a shared read lock.
    ///     Passed to <see cref="CreateInstance" />.
    /// </param>
    /// <param name="cancellationToken">Cancels the lock wait.</param>
    /// <returns>
    ///     A lease whose <see cref="Lease{T}.Value" /> is the newly opened resource.
    ///     Dispose to release the lock and close the resource.
    /// </returns>
    public async ValueTask<Lease<T>> AcquireAsync(
        string filePath,
        bool writeable,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        
        IDisposable? releaser = null;
        if (writeable)
            releaser = await _writeLocker.LockAsync(normalizedPath, cancellationToken).ConfigureAwait(false);
        
        var value = CreateInstance(normalizedPath, writeable);
        return new Lease<T>(releaser, value);
    }

    /// <summary>
    ///     Opens a new resource instance for <paramref name="normalizedPath" />.
    /// </summary>
    protected abstract T CreateInstance(string normalizedPath, bool isEditable);
}