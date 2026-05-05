using SlideGenerator.Ipc.Ipc;
using SlideGenerator.Pipelines.Generating.Models;
using SlideGenerator.Pipelines.Generating.Workflows;
using SlideGenerator.Pipelines.Generating.Workflows.Models;
using WorkflowCore.Interface;

namespace SlideGenerator.Ipc.Handlers;

/// <summary>
///     Handles all <c>workflow.*</c> JSON-RPC methods: start, cancel, pause, and resume.
///     Delegates execution to the WorkflowCore <see cref="IWorkflowHost" /> and
///     <see cref="IWorkflowController" />, and publishes progress events to <see cref="WorkflowEventBus" />.
/// </summary>
public sealed class WorkflowHandler(
    IWorkflowHost workflowHost,
    IWorkflowController workflowController,
    WorkflowEventBus eventBus)
{
    /// <summary>
    ///     Starts a new slide generation workflow from the provided request and returns its instance ID.
    /// </summary>
    /// <param name="request">
    ///     The generation request deserialized directly from the JSON-RPC payload,
    ///     containing the recipe, output type, and save folder configuration.
    /// </param>
    /// <param name="ct">A cancellation token that, when canceled, aborts the start operation.</param>
    /// <returns>
    ///     The unique identifier of the newly created workflow instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="request" /> contains an empty recipe or missing save folder.
    /// </exception>
    public async Task<string> StartAsync(GeneratingRequest request, CancellationToken ct)
    {
        var task = new GeneratingTask { Request = request };

        var instanceId = await workflowHost
            .StartWorkflow(nameof(GeneratingWorkflow), 1, task)
            .ConfigureAwait(false);

        Publish(instanceId, "WorkflowStarted", null, "Running");

        return instanceId;
    }

    /// <summary>
    ///     Terminates a running workflow instance. In-progress steps run to completion before
    ///     the workflow is marked as terminated.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance identifier to cancel.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="bool" /> indicating whether the termination was accepted.</returns>
    public async Task<bool> CancelAsync(string workflowInstanceId, CancellationToken ct)
    {
        var success = await workflowController
            .TerminateWorkflow(workflowInstanceId)
            .ConfigureAwait(false);

        if (success)
            Publish(workflowInstanceId, "WorkflowCancelled", null, "Cancelled");

        return success;
    }

    /// <summary>
    ///     Suspends a running workflow instance. The workflow can be resumed later via
    ///     <c>workflow.resume</c>.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance identifier to pause.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="bool" /> indicating whether the suspension was accepted.</returns>
    public async Task<bool> PauseAsync(string workflowInstanceId, CancellationToken ct)
    {
        var success = await workflowController
            .SuspendWorkflow(workflowInstanceId)
            .ConfigureAwait(false);

        if (success)
            Publish(workflowInstanceId, "WorkflowSuspended", null, "Paused");

        return success;
    }

    /// <summary>
    ///     Resumes a previously suspended workflow instance from the point at which it was suspended.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance identifier to resume.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="bool" /> indicating whether the resumption was accepted.</returns>
    public async Task<bool> ResumeAsync(string workflowInstanceId, CancellationToken ct)
    {
        var success = await workflowController
            .ResumeWorkflow(workflowInstanceId)
            .ConfigureAwait(false);

        if (success)
            Publish(workflowInstanceId, "WorkflowResumed", null, "Running");

        return success;
    }

    private void Publish(string instanceId, string eventName, string? stepName, string status)
    {
        eventBus.Publish(new WorkflowProgress
        {
            WorkflowInstanceId = instanceId,
            Event = eventName,
            StepName = stepName,
            Phase = null,
            Status = status,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}