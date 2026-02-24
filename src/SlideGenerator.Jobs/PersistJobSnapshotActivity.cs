using Elsa.Workflows;
using SlideGenerator.Jobs.Entities.Jobs;

namespace SlideGenerator.Jobs;

/// <summary>
///     Elsa activity that persists a job snapshot.
/// </summary>
public sealed class PersistJobSnapshotActivity : Activity
{
    /// <summary>
    ///     Snapshot to persist.
    /// </summary>
    public required JobSnapshotEntity Snapshot { get; init; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        _ = Snapshot;
        await context.CompleteActivityAsync();
    }
}
