using Elsa.Workflows;
using Elsa.Workflows.Options;
using Microsoft.Extensions.Logging;
using SlideGenerator.Jobs.Entities.Jobs;

namespace SlideGenerator.Jobs;

/// <summary>
///     Dispatches job snapshots through Elsa workflow runner for persistence.
/// </summary>
public sealed class JobSnapshotWorkflowDispatcher
{
    private readonly ILogger<JobSnapshotWorkflowDispatcher> _logger;
    private readonly IWorkflowRunner _workflowRunner;

    /// <summary>
    ///     Creates a snapshot workflow dispatcher.
    /// </summary>
    public JobSnapshotWorkflowDispatcher(IWorkflowRunner workflowRunner, ILogger<JobSnapshotWorkflowDispatcher> logger)
    {
        _workflowRunner = workflowRunner;
        _logger = logger;
    }

    /// <summary>
    ///     Persists the specified snapshot through Elsa workflow runtime.
    /// </summary>
    public async Task PersistAsync(JobSnapshotEntity snapshot, CancellationToken cancellationToken)
    {
        var activity = new PersistJobSnapshotActivity
        {
            Snapshot = snapshot
        };

        try
        {
            await _workflowRunner.RunAsync(activity, new RunWorkflowOptions(), cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to persist snapshot for job {JobId} via Elsa workflow", snapshot.JobId);
        }
    }
}
