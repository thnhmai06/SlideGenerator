namespace SlideGenerator.Domain.Tasks.Models;

/// <summary>
///     Status of a job (group or sheet level).
/// </summary>
public enum TaskStatus
{
    /// <summary>
    ///     Job is waiting to start.
    /// </summary>
    Pending,

    /// <summary>
    ///     Job is actively processing.
    /// </summary>
    Running,

    /// <summary>
    ///     Job is temporarily paused.
    /// </summary>
    Paused,

    /// <summary>
    ///     Job completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    ///     Job failed with error.
    /// </summary>
    Error,

    /// <summary>
    ///     Job was cancelled by user.
    /// </summary>
    Cancelled
}

