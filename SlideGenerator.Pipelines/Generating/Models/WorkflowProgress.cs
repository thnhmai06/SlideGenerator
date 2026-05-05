namespace SlideGenerator.Pipelines.Generating.Models;

/// <summary>
///     Represents the progress information of a running workflow.
/// </summary>
public sealed record WorkflowProgress
{
    /// <summary>Gets the workflow instance identifier that this event belongs to.</summary>
    public string WorkflowInstanceId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the lifecycle event type. Possible values:
    ///     <c>WorkflowStarted</c>, <c>WorkflowCompleted</c>, <c>WorkflowSuspended</c>,
    ///     <c>WorkflowResumed</c>, <c>WorkflowError</c>, <c>StepCompleted</c>.
    /// </summary>
    public string Event { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the class name of the completed step (e.g., <c>"DownloadImage"</c>),
    ///     or <see langword="null" /> for workflow-level events.
    /// </summary>
    public string? StepName { get; init; }

    /// <summary>
    ///     Gets the workflow phase the step belongs to (<c>"PhaseA"</c>, <c>"PhaseB"</c>,
    ///     or <c>"PhaseC"</c>), or <see langword="null" /> for workflow-level events.
    /// </summary>
    public string? Phase { get; init; }

    /// <summary>
    ///     Gets the current workflow status. Possible values:
    ///     <c>Running</c>, <c>Complete</c>, <c>Paused</c>, <c>Error</c>.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets the UTC timestamp when this event was emitted.</summary>
    public DateTimeOffset Timestamp { get; init; }
}
