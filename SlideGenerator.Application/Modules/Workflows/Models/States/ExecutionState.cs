using SlideGenerator.Application.Modules.Workflows.Entities.Contexts;
using SlideGenerator.Application.Modules.Workflows.Models.Logging;

namespace SlideGenerator.Application.Modules.Workflows.Models.States;

/// <summary>
///     Provides a common base for tracking the execution state of workflows and activities.
/// </summary>
public abstract class ExecutionState(
    string id,
    IExecutionContext executionContext,
    IReadOnlyDictionary<string, ExecutionState>? activities = null)
{
    private static readonly HashSet<string> MsgFinished = ["[[COMPLETED]]", "[[FINISHED]]"];
    private static readonly HashSet<string> MsgPaused = ["[[PAUSED]]"];
    private static readonly HashSet<string> MsgCancelled = ["[[CANCELLED]]", "[[CANCELED]]"];

    /// <summary>Gets the unique identifier for this execution unit.</summary>
    public string Id { get; } = id;

    /// <summary>Gets the execution context (variables and services) for this scope.</summary>
    public IExecutionContext ExecutionContext { get; } = executionContext;

    /// <summary>Gets or sets the current execution status.</summary>
    public Status Status { get; set; } = Status.Queued;

    /// <summary>Gets the logger for this execution unit.</summary>
    public Logger Logger { get; } = new();

    /// <summary>Gets the collection of child activities.</summary>
    public IReadOnlyDictionary<string, ExecutionState> Activities { get; } =
        activities ?? new Dictionary<string, ExecutionState>();

    /// <summary>Retrieves a child activity state by its ID.</summary>
    /// <param name="activityId">The ID of the activity to retrieve.</param>
    /// <returns>The activity state if found; otherwise, <see langword="null" />.</returns>
    public ExecutionState? GetActivity(string activityId)
    {
        return Activities.GetValueOrDefault(activityId);
    }

    /// <summary>Retrieves a child activity state of a specific type by its ID.</summary>
    /// <typeparam name="T">The type of the activity state.</typeparam>
    /// <param name="activityId">The ID of the activity to retrieve.</param>
    /// <returns>The activity state if found and matches the type; otherwise, <see langword="null" />.</returns>
    public T? GetActivity<T>(string activityId) where T : ExecutionState
    {
        return GetActivity(activityId) as T;
    }

    /// <summary>Synchronizes the status based on the logs and children's states.</summary>
    public virtual void SyncStatus()
    {
        // 1. Sync children first (bottom-up)
        foreach (var child in Activities.Values) child.SyncStatus();

        // 2. Aggregate from children if any
        if (Activities.Count > 0)
        {
            Status = Activities.Values.Select(a => a.Status).InferState();
            return;
        }

        // 3. Heuristic from logs for leaf nodes
        if (Logger.Logs.Any(l => l.Level == LogLevel.Error))
        {
            Status = Status.Faulted;
            return;
        }

        var latestEntry = Logger.Logs.MaxBy(l => l.Timestamp);
        if (latestEntry == null) return;

        var message = latestEntry.Message.ToUpperInvariant();
        if (MsgFinished.Any(m => message.Contains(m))) Status = Status.Finished;
        else if (MsgPaused.Any(m => message.Contains(m))) Status = Status.Paused;
        else if (MsgCancelled.Any(m => message.Contains(m))) Status = Status.Canceled;
        else Status = Status.Running;
    }
}