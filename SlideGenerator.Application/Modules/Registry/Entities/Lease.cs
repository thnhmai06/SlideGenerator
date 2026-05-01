namespace SlideGenerator.Application.Modules.Registry.Entities;

/// <summary>
///     A disposable lease over a registry-managed resource.
///     Disposing releases the lock and disposes the resource (if it implements <see cref="IDisposable" />).
/// </summary>
public sealed class Lease<TValue> : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     The lock handle associated with this lease, if any.
    /// </summary>
    private readonly IDisposable? _handle;

    /// <summary>
    ///     Indicates whether the lease has been disposed (0 = false, 1 = true).
    /// </summary>
    private int _disposed;

    internal Lease(IDisposable? handle, TValue value)
    {
        _handle = handle;
        Value = value;
    }

    /// <summary>Gets the leased resource instance.</summary>
    public TValue Value { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _handle?.Dispose();

        if (Value is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            (Value as IDisposable)?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _handle?.Dispose();
        (Value as IDisposable)?.Dispose();
    }
}
