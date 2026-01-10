namespace SlideGenerator.Domain.Features.Jobs.Components;

/// <summary>
///     Cooperative pause controller for job execution.
/// </summary>
public sealed class PauseSignal
{
    private volatile TaskCompletionSource<bool>? _pauseSource;

    /// <summary>
    ///     Gets a value indicating whether the signal is paused.
    /// </summary>
    public bool IsPaused => _pauseSource != null;

    /// <summary>
    ///     Requests a pause at the next checkpoint.
    /// </summary>
    public void Pause()
    {
        if (_pauseSource != null) return;
        _pauseSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    ///     Resumes execution from a paused state.
    /// </summary>
    public void Resume()
    {
        var source = _pauseSource;
        if (source == null) return;
        _pauseSource = null;
        source.TrySetResult(true);
    }

    /// <summary>
    ///     Exits the current execution if paused.
    /// </summary>
    public Task WaitIfPausedAsync(CancellationToken cancellationToken)
    {
        var source = _pauseSource;
        if (source == null) return Task.CompletedTask;
        return Task.FromException(new OperationCanceledException("Job paused."));
    }
}