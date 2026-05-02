namespace SlideGenerator.Registry.Entities;

/// <summary>
///     Represents a temporary lease on a resource with an associated lock.
///     When disposed, the lock is released and the resource is disposed if it implements <see cref="IDisposable" />.
/// </summary>
/// <typeparam name="T">The type of the resource.</typeparam>
public sealed class Lease<T>(IDisposable? releaser, T value) : IDisposable where T : IDisposable
{
    /// <summary>
    ///     Gets the resource instance held by the lease.
    /// </summary>
    public T Value { get; } = value;

    /// <inheritdoc />
    public void Dispose()
    {
        releaser?.Dispose();
        Value.Dispose();
    }
}