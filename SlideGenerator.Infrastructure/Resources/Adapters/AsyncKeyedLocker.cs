using AsyncKeyedLock;
using SlideGenerator.Application.Resources.Abstractions;

namespace SlideGenerator.Infrastructure.Resources.Adapters;

/// <summary>
///     <see cref="IAsyncKeyedLocker{TKey}" /> implementation backed by
///     <see cref="AsyncKeyedLock.AsyncKeyedLocker{TKey}" /> from the <c>AsyncKeyedLock</c> package.
///     Each call to <see cref="LockAsync" /> waits on the per-key
///     <see cref="System.Threading.SemaphoreSlim" />; the slim is automatically returned to the
///     pool (and disposed) by the library when the count returns to its maximum value,
///     i.e., when no more holders or waiters remain for that key.
/// </summary>
/// <typeparam name="TKey">The lock key type.</typeparam>
public sealed class AsyncKeyedLocker<TKey> : IAsyncKeyedLocker<TKey>
    where TKey : notnull
{
    private readonly AsyncKeyedLock.AsyncKeyedLocker<TKey> _locker;

    /// <summary>
    ///     Initializes the adapter.
    /// </summary>
    /// <param name="configure">
    ///     Optional delegate to configure <see cref="AsyncKeyedLockOptions" />
    ///     (pool size, max-count, etc.).  When <see langword="null" />, the library defaults are
    ///     used.  Pass <c>opt =&gt; opt.MaxCount = <see cref="int.MaxValue" /></c> for registries
    ///     where the per-key slim must never block (e.g., read-only workbooks or presentations
    ///     whose concurrency is gated externally by <see cref="SemaphoreSlimRegistry" />).
    /// </param>
    public AsyncKeyedLocker(Action<AsyncKeyedLockOptions>? configure = null)
    {
        _locker = configure is null
            ? new AsyncKeyedLock.AsyncKeyedLocker<TKey>()
            : new AsyncKeyedLock.AsyncKeyedLocker<TKey>(configure);
    }

    /// <inheritdoc />
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
    public void Dispose() => _locker.Dispose();

    // Wraps the library releaser behind IDisposable so the caller never references AsyncKeyedLock types.
    private sealed class Handle(IDisposable releaser) : IKeyedLockHandle
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            releaser.Dispose();
        }
    }
}
