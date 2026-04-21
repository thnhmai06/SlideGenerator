namespace SlideGenerator.Application.Resources.Abstractions;

/// <summary>
///     Provides per-key asynchronous locking backed by <see cref="System.Threading.SemaphoreSlim" /> instances.
///     Each key owns its own slim; the slim is created on demand and auto-disposed by the implementation
///     when no more holders remain for that key.
/// </summary>
/// <typeparam name="TKey">The type used as the lock key.</typeparam>
public interface IAsyncKeyedLocker<in TKey> : IDisposable
    where TKey : notnull
{
    /// <summary>
    ///     Asynchronously acquires the per-key slim.
    ///     The maximum concurrency for each key is determined by the implementation at construction time
    ///     (e.g., via a factory delegate or fixed option).
    /// </summary>
    /// <param name="key">The key whose slim to acquire.</param>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    /// <returns>
    ///     A handle that releases one slim permit when disposed. Dispose it exactly once;
    ///     further calls are no-ops.
    /// </returns>
    ValueTask<IKeyedLockHandle> LockAsync(
        TKey key,
        CancellationToken cancellationToken = default);
}
