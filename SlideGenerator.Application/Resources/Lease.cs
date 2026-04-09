namespace SlideGenerator.Application.Resources;

/// <summary>
///     Provides a thread-safe base implementation for leases that release resources once on disposal.
/// </summary>
public abstract class Lease : IDisposable
{
    private int _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Release();
    }

    /// <summary>
    ///     Releases the underlying resource exactly once.
    /// </summary>
    protected abstract void Release();
}