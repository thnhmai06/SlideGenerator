using SlideGenerator.Application.Modules.Workflows.Models.Logging;
using ExecutionContext = SlideGenerator.Application.Modules.Workflows.Models.States.IExecutionContext;

namespace SlideGenerator.Application.Modules.Workflows.Models.States;

/// <summary>
///     Common base for tracking the execution status of workflows and activities.
/// </summary>
public abstract class ExecutionSnapshot(
    string id,
    IExecutionPayload payload,
    ExecutionContext? context = null,
    IReadOnlyDictionary<string, ExecutionSnapshot>? activities = null)
{
    private static readonly HashSet<string> MsgFinished = ["[[COMPLETED]]", "[[FINISHED]]"];
    private static readonly HashSet<string> MsgPaused = ["[[PAUSED]]"];
    private static readonly HashSet<string> MsgCancelled = ["[[CANCELLED]]", "[[CANCELED]]"];

    /// <summary>Gets the unique identifier for this execution unit.</summary>
    public string Id { get; } = id;

    /// <summary>Gets the checkpoint-able variable payload for this scope.</summary>
    public IExecutionPayload Payload { get; } = payload;

    /// <summary>Gets the transient execution context carrying runtime-only state.</summary>
    public ExecutionContext? Context { get; } = context;

    /// <summary>Gets or sets the current execution status.</summary>
    public Status Status { get; set; } = Status.Queued;

    /// <summary>Gets the logger for this execution unit.</summary>
    public Logger Logger { get; } = new();

    /// <summary>Gets the collection of child activity snapshots.</summary>
    public IReadOnlyDictionary<string, ExecutionSnapshot> Activities { get; } =
        activities ?? new Dictionary<string, ExecutionSnapshot>();

    /// <summary>Retrieves a child activity snapshot by its ID.</summary>
    public ExecutionSnapshot? GetActivity(string activityId) =>
        Activities.GetValueOrDefault(activityId);

    /// <summary>Retrieves a child activity snapshot of a specific type by its ID.</summary>
    public T? GetActivity<T>(string activityId) where T : ExecutionSnapshot =>
        GetActivity(activityId) as T;

    /// <summary>Synchronizes the status based on logs and children's states.</summary>
    public virtual void SyncStatus()
    {
        foreach (var child in Activities.Values) child.SyncStatus();

        if (Activities.Count > 0)
        {
            Status = Activities.Values.Select(a => a.Status).InferState();
            return;
        }

        if (Logger.Logs.Any(l => l.Level == LogLevel.Error))
        {
            Status = Status.Faulted;
            return;
        }

        var latestEntry = Logger.Logs.MaxBy(l => l.Timestamp);
        var message = latestEntry.Message.ToUpperInvariant();
        if (MsgFinished.Any(m => message.Contains(m))) Status = Status.Finished;
        else if (MsgPaused.Any(m => message.Contains(m))) Status = Status.Paused;
        else if (MsgCancelled.Any(m => message.Contains(m))) Status = Status.Canceled;
        else Status = Status.Running;
    }
}
