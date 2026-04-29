using SlideGenerator.Application.Modules.Resources.Interfaces;

namespace SlideGenerator.Application.Modules.Resources.Entities;

/// <summary>
///     A disposable lease over a registry-managed resource.
///     Disposing releases the lock and disposes the resource (if it implements <see cref="IDisposable" />).
/// </summary>
public sealed class Lease<TValue> : IDisposable, IAsyncDisposable
{
    private readonly ILock _handle;
    private int _disposed;

    internal Lease(ILock handle, TValue value)
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
        _handle.Dispose();

        if (Value is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            (Value as IDisposable)?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _handle.Dispose();
        (Value as IDisposable)?.Dispose();
    }
}