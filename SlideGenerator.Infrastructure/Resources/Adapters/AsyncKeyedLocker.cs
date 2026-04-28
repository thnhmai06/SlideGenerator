using AsyncKeyedLock;
using SlideGenerator.Application.Modules.Resources.Abstractions;

namespace SlideGenerator.Infrastructure.Resources.Adapters;

/// <summary>
///     <see cref="IAsyncKeyedLocker{TKey}" /> implementation backed by
///     <see cref="AsyncKeyedLock.AsyncKeyedLocker{TKey}" /> from the <c>AsyncKeyedLock</c> package.
/// </summary>
/// <remarks>
///     Each call to <see cref="LockAsync" /> waits on the per-key <see cref="System.Threading.SemaphoreSlim" />;
///     the slim is automatically returned to the pool (and disposed) by the library when the count returns to its maximum
///     value,
///     i.e., when no more holders or waiters remain for that key.
/// </remarks>
/// <typeparam name="TKey">The lock key type.</typeparam>
public sealed class AsyncKeyedLocker<TKey> : IAsyncKeyedLocker<TKey>
    where TKey : notnull
{
    /// <summary>
    ///     The underlying <see cref="AsyncKeyedLock.AsyncKeyedLocker{TKey}" /> instance.
    /// </summary>
    private readonly AsyncKeyedLock.AsyncKeyedLocker<TKey> _locker;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncKeyedLocker{TKey}" /> class.
    /// </summary>
    /// <param name="configure">
    ///     Optional delegate to configure <see cref="AsyncKeyedLockOptions" /> (pool size, max-count, etc.).
    ///     When <see langword="null" />, the library defaults are used.
    /// </param>
    public AsyncKeyedLocker(Action<AsyncKeyedLockOptions>? configure = null)
    {
        _locker = configure is null
            ? new AsyncKeyedLock.AsyncKeyedLocker<TKey>()
            : new AsyncKeyedLock.AsyncKeyedLocker<TKey>(configure);
    }

    /// <inheritdoc />
    /// <summary>
    ///     Asynchronously acquires an exclusive lock for the specified key.
    /// </summary>
    /// <param name="key">The key to lock.</param>
    /// <param name="cancellationToken">A token to cancel the lock operation.</param>
    /// <returns>A task yielding an <see cref="IKeyedLockHandle" /> that releases the lock when disposed.</returns>
    public async ValueTask<IKeyedLockHandle> LockAsync(
        TKey key,
        CancellationToken cancellationToken = default)
    {
        var releaser = await _locker
            .LockAsync(key, cancellationToken)
            .ConfigureAwait(false);

        return new Handle(releaser);
    }

    /// <inheritdoc />
    /// <summary>
    ///     Disposes the underlying locker and releases all associated resources.
    /// </summary>
    public void Dispose()
    {
        _locker.Dispose();
    }

    /// <summary>
    ///     Wraps the library-specific releaser to implement <see cref="IKeyedLockHandle" />.
    /// </summary>
    /// <param name="releaser">The underlying <see cref="IDisposable" /> that releases the lock.</param>
    private sealed class Handle(IDisposable releaser) : IKeyedLockHandle
    {
        /// <summary>
        ///     Track if the handle has been disposed to avoid double disposal.
        /// </summary>
        private int _disposed;

        /// <inheritdoc />
        /// <summary>
        ///     Releases the acquired lock.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            releaser.Dispose();
        }
    }
}