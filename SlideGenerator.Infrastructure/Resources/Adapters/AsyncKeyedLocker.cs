using AsyncKeyedLock;
using SlideGenerator.Application.Modules.Resources.Interfaces;

namespace SlideGenerator.Infrastructure.Resources.Adapters;

/// <summary>
///     Generic <see cref="ILocker{TKey}" /> adapter backed by the <c>AsyncKeyedLock</c> library.
/// </summary>
/// <typeparam name="TKey">The type of the lock key.</typeparam>
public sealed class AsyncKeyedLocker<TKey>(AsyncKeyedLockOptions? configure = null)
    : ILocker<TKey>
    where TKey : notnull
{
    private readonly global::AsyncKeyedLock.AsyncKeyedLocker<TKey> _locker = configure is null
        ? new global::AsyncKeyedLock.AsyncKeyedLocker<TKey>()
        : new global::AsyncKeyedLock.AsyncKeyedLocker<TKey>(configure);

    /// <inheritdoc />
    public void Dispose()
    {
        _locker.Dispose();
    }

    public async ValueTask<ILock> AcquireAsync(TKey key, CancellationToken cancellationToken)
    {
        var releaser = await _locker
            .LockAsync(key, cancellationToken)
            .ConfigureAwait(false);

        return new AsyncKeyedLock(releaser);
    }
}