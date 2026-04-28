using SlideGenerator.Application.Workflows.DSL;
using SlideGenerator.Application.Workflows.Models.States;

namespace SlideGenerator.Application.Workflows.Abstractions;

/// <summary>
///     Provides lifecycle management for workflow instances: starting, pausing, resuming, and cancelling runs.
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    ///     Starts a new workflow instance with the supplied data and returns its assigned identifier.
    /// </summary>
    Task<string> RunAsync<TDef, TData>(TData data, CancellationToken ct = default)
        where TDef : IWorkflowDefinition<TData>, new()
        where TData : class, new();

    /// <summary>Suspends the workflow with the given identifier.</summary>
    Task PauseAsync(string id);

    /// <summary>Resumes a previously paused workflow.</summary>
    Task ResumeAsync(string id);

    /// <summary>Cancels and terminates the workflow with the given identifier.</summary>
    Task CancelAsync(string id);

    /// <summary>Returns all currently active workflow state snapshots.</summary>
    IEnumerable<WorkflowState> Workflows { get; }

    /// <summary>Returns the state snapshot for a specific workflow, or <see langword="null" /> if not found.</summary>
    Task<WorkflowState?> GetWorkflow(string id);
}