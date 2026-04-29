using SlideGenerator.Application.Modules.Resources.Entities;

namespace SlideGenerator.Application.Modules.Resources.Services;

/// <summary>
///     Async registry specialized for file-backed resources.
///     Keys are normalized to full paths and compared case-insensitively (ordinal).
/// </summary>
/// <typeparam name="T">The resource type stored by the registry.</typeparam>
/// <param name="locker">
///     Per-key reader-writer locker supplied by the DI container.
///     Read acquires are shared; write acquires are exclusive.
/// </param>
public abstract class FileRegistry<T>(FileLocker locker)
    : Registry<string, T>(locker.ReadLockAsync, locker.WriteLockAsync)
{
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
    public ValueTask<Lease<T>> AcquireAsync(
        string filePath,
        bool writeable,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = FormatKey(filePath);
        return AcquireAsync(
            normalizedPath,
            k => new ValueTask<T>(CreateInstance(k, writeable)),
            writeable,
            cancellationToken);
    }

    /// <summary>
    ///     Synchronous overload for use in non-async contexts (e.g., <see cref="Settings" /> managers).
    ///     Safe when <paramref name="isEditable" /> is <see langword="false" /> (read lock is non-blocking
    ///     when no writer holds it) or when the caller accepts the blocking semantics of a write lock.
    /// </summary>
    /// <param name="filePath">File path used as the registry key (normalized internally).</param>
    /// <param name="isEditable">Whether the resource should be opened for editing.</param>
    /// <returns>A lease whose <see cref="Lease{T}.Value" /> is the open resource.</returns>
    public async Task<Lease<T>> Acquire(string filePath, bool isEditable)
    {
        return await AcquireAsync(filePath, isEditable).ConfigureAwait(false);
    }

    /// <summary>Normalizes <paramref name="filePath" /> to the canonical registry key.</summary>
    protected override string FormatKey(string filePath) => Path.GetFullPath(filePath);

    /// <summary>
    ///     Opens a new resource instance for <paramref name="normalizedPath" />.
    ///     Called once per <see cref="AcquireAsync" /> invocation — there is no caching at this layer.
    /// </summary>
    /// <param name="normalizedPath">The canonical registry key.</param>
    /// <param name="isEditable">Whether the resource should be opened for editing.</param>
    /// <returns>The newly opened resource instance.</returns>
    protected abstract T CreateInstance(string normalizedPath, bool isEditable);
}
