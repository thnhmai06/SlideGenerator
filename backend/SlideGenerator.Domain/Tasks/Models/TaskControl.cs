namespace SlideGenerator.Domain.Tasks.Models;

/// <summary>
///     Control actions that can be applied to a job.
/// </summary>
public enum TaskControl
{
    /// <summary>
    ///     Pause the job at the next checkpoint.
    /// </summary>
    Pause,

    /// <summary>
    ///     Resume a paused job.
    /// </summary>
    Resume,

    /// <summary>
    ///     Cancel the job immediately.
    /// </summary>
    Cancel
}