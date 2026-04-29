using SlideGenerator.Application.Modules.Resources.Interfaces;

namespace SlideGenerator.Infrastructure.Resources.Adapters;

/// <summary>
///     Provides an implementation of <see cref="ILock" /> that wraps a releaser object and enables
///     proper disposal of the lock.
/// </summary>
/// <remarks>
///     This class is used internally to represent an acquired lock in the <see cref="AsyncKeyedLocker{TKey}" />.
///     It ensures that the underlying lock releaser is disposed when the instance itself is disposed.
/// </remarks>
internal sealed class AsyncKeyedLock(IDisposable releaser) : ILock
{
    private int _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        releaser.Dispose();
    }
}