using SlideGenerator.Application.Resources.Abstractions;

namespace SlideGenerator.Application.Resources.Services;

/// <summary>
///     Async registry specialized for file-backed resources.
///     Keys are normalized to full paths and compared case-insensitively (ordinal).
/// </summary>
/// <typeparam name="T">The resource type stored by the registry.</typeparam>
/// <param name="locker">
///     Per-key locker supplied by the DI container.  Concurrency limits are configured on
///     the locker at construction time rather than per-call.
/// </param>
public abstract class FileRegistry<T>(IAsyncKeyedLocker<string> locker)
    : Registry<string, T>(locker, StringComparer.OrdinalIgnoreCase)
{
    /// <summary>
    ///     Acquires the per-path slim and returns a lease over the resource, opening it if necessary.
    /// </summary>
    /// <param name="filePath">File path used as the registry key (normalized internally).</param>
    /// <param name="isEditable">Passed to <see cref="OpenResource" /> for the first caller on a new entry.</param>
    /// <param name="cancellationToken">Cancels the slim wait.</param>
    /// <returns>
    ///     A lease whose <see cref="Registry{TKey,TValue}.Lease.Value" /> is the open resource.
    ///     Dispose to release the slim permit; the resource is disposed when the last holder exits.
    /// </returns>
    public ValueTask<Lease> AcquireAsync(
        string filePath,
        bool isEditable,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizeKey(filePath);
        return AcquireAsync(
            normalizedPath,
            k => new ValueTask<T>(OpenResource(k, isEditable)),
            cancellationToken);
    }

    /// <summary>
    ///     Synchronous overload for use in non-async delegates (e.g., Elsa <c>Input&lt;T&gt;</c> lambdas).
    ///     Safe only when the locker's <c>MaxCount</c> is large enough that the underlying slim never
    ///     blocks (e.g., <see cref="int.MaxValue" /> for read-only workbooks).
    /// </summary>
    /// <param name="filePath">File path used as the registry key (normalized internally).</param>
    /// <param name="isEditable">Passed to <see cref="OpenResource" /> for the first caller on a new entry.</param>
    /// <returns>A lease whose <see cref="Registry{TKey,TValue}.Lease.Value" /> is the open resource.</returns>
    public async Task<Lease> Acquire(string filePath, bool isEditable)
        => await AcquireAsync(filePath, isEditable);

    /// <summary>Normalizes <paramref name="filePath" /> to the canonical registry key.</summary>
    protected override string NormalizeKey(string filePath) => Path.GetFullPath(filePath);

    /// <summary>
    ///     Opens a new resource instance for <paramref name="normalizedPath" />.
    ///     Called at most once per live entry (while the internal lock is held).
    /// </summary>
    /// <param name="normalizedPath">The canonical registry key.</param>
    /// <param name="isEditable">Whether the resource should be opened for editing.</param>
    /// <returns>The newly opened resource instance.</returns>
    protected abstract T OpenResource(string normalizedPath, bool isEditable);
}
