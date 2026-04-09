namespace SlideGenerator.Application.Resources;

/// <summary>
///     Represents a disposable lease over a registry-managed resource.
/// </summary>
/// <typeparam name="T">The resource type held by the lease.</typeparam>
public sealed class RegistryLease<T>(T value, Action? releaseAction) : Lease
{
    /// <summary>
    ///     Gets the leased resource instance.
    /// </summary>
    public T Value { get; } = value;

    /// <inheritdoc />
    protected override void Release()
    {
        releaseAction?.Invoke();
    }
}