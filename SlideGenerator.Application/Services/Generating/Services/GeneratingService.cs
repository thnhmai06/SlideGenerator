using SlideGenerator.Application.Modules.Workflows.Abstractions;
using SlideGenerator.Application.Modules.Workflows.Models.States;
using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Generating.Models.States;
using SlideGenerator.Application.Services.Generating.Workflows;
using SlideGenerator.Application.Services.Generating.Workflows.Models;

namespace SlideGenerator.Application.Services.Generating.Services;

/// <summary>
///     Application service for generating presentations.
///     Coordinates workflow execution and exposes job status via IWorkflowService.
/// </summary>
public sealed class GeneratingService(IWorkflowService workflowService)
{
    // TODO: Implement workflow execution
    
    /// <summary>Starts a generation job and returns its instance ID.</summary>
    public Task<string> RunAsync(GeneratingRequest request, CancellationToken ct = default)
        => workflowService.RunAsync<GeneratingWorkflow, WorkflowTask>(
            new WorkflowTask { Request = request }, ct);

    /// <summary>Suspends an active generation job.</summary>
    public Task PauseAsync(string id) => workflowService.PauseAsync(id);

    /// <summary>Resumes a suspended generation job.</summary>
    public Task ResumeAsync(string id) => workflowService.ResumeAsync(id);

    /// <summary>Cancels an active generation job.</summary>
    public Task CancelAsync(string id) => workflowService.CancelAsync(id);

    /// <summary>Exposes active workflow states.</summary>
    public IEnumerable<WorkflowState> Workflows => workflowService.Workflows;

    /// <summary>Resolves a complete generating workflow state snapshot.</summary>
    public async Task<GeneratingState?> GetWorkbookAsync(string instanceId, CancellationToken ct = default)
    {
        var state = await workflowService.GetWorkflow(instanceId).ConfigureAwait(false);
        return state as GeneratingState;
    }
}
