namespace SlideGenerator.Application.Common;

/// <summary>
///     Represents a disposable lease over a registry-managed resource.
/// </summary>
/// <typeparam name="T">The resource type held by the lease.</typeparam>
public readonly struct RegistryLease<T>(T value, Action? releaseAction) : IDisposable
{
    /// <summary>
    ///     Gets the leased resource instance.
    /// </summary>
    public T Value { get; } = value;

    private readonly Action? _releaseAction = releaseAction;

    /// <summary>
    ///     Releases the lease back to the owning registry.
    /// </summary>
    public void Dispose()
    {
        _releaseAction?.Invoke();
    }
}