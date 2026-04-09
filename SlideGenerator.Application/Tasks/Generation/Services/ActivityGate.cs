namespace SlideGenerator.Application.Tasks.Generation.Services;

/// <summary>
///     Coordinates the maximum number of activity executions that may run concurrently in a flow.
/// </summary>
/// <remarks>
///     This gate is intentionally process-wide so that multiple Elsa workflow instances share the same admission limit.
/// </remarks>
public sealed class ActivityGate : IDisposable
{
    /// <summary>
    ///     The default maximum concurrency used when no valid setting value is provided.
    /// </summary>
    public const int DefaultMaximumConcurrency = 5;

    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    ///     Creates a new gate with the specified concurrency limit.
    /// </summary>
    /// <param name="maximumConcurrency">The maximum number of activity executions that may run at the same time.</param>
    public ActivityGate(int maximumConcurrency = DefaultMaximumConcurrency)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumConcurrency, 1);

        MaximumConcurrency = maximumConcurrency;
        _semaphore = new SemaphoreSlim(maximumConcurrency, maximumConcurrency);
    }

    /// <summary>
    ///     Gets the configured maximum number of parallel activity executions.
    /// </summary>
    public int MaximumConcurrency { get; }

    /// <summary>
    ///     Gets the number of currently available activity execution permits.
    /// </summary>
    public int AvailableSlots => _semaphore.CurrentCount;

    /// <summary>
    ///     Acquires one activity execution permit and returns a lease that releases it when disposed.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the wait operation.</param>
    /// <returns>A lease representing the acquired permit.</returns>
    public async ValueTask<ActivityLease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new ActivityLease(this);
    }

    internal void Release()
    {
        _semaphore.Release();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore.Dispose();
    }
}