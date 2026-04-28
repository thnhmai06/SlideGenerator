namespace SlideGenerator.Application.Modules.Workflows.Models;

/// <summary>
///     Specifies the execution state of a job at any level (Row, Worksheet, or Workbook).
/// </summary>
public enum Status
{
    // note: @thnhmai06 added emojis while waiting for Gemini generates code at 15:23 24/04/2026 GMT+7, not AI did.

    /// <summary>⏳ The job is waiting in the queue.</summary>
    Queued,

    /// <summary>🏃 The job is currently being executed.</summary>
    Running,

    /// <summary>⏸️ The job has been temporarily suspended.</summary>
    Paused,

    /// <summary>🏁 The job has completed successfully.</summary>
    Finished,

    /// <summary>⛔ The job failed due to an error.</summary>
    Faulted,

    /// <summary>❌ The job was explicitly canceled by the user or system.</summary>
    Canceled
}

/// <summary>
///     Provides extension methods for <see cref="Status" />.
/// </summary>
public static class StatusExtensions
{
    /// <summary>
    ///     Infers the aggregated state from a collection of child states.
    /// </summary>
    public static Status InferState(this IEnumerable<Status> states)
    {
        var stateList = states.ToList();
        if (stateList.Count == 0) return Status.Finished;

        if (stateList.Any(s => s == Status.Faulted)) return Status.Faulted;
        if (stateList.Any(s => s == Status.Canceled)) return Status.Canceled;
        if (stateList.Any(s => s == Status.Running)) return Status.Running;
        if (stateList.Any(s => s == Status.Paused)) return Status.Paused;
        if (stateList.All(s => s == Status.Queued)) return Status.Queued;
        if (stateList.All(s => s == Status.Finished)) return Status.Finished;

        return Status.Running;
    }
}