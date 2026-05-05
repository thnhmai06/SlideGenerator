using SlideGenerator.Pipelines.Generating.Models;

namespace SlideGenerator.Ipc.Ipc;

/// <summary>
///     A lightweight, in-process event bus for workflow progress notifications.
///     Decouples the <see cref="Handlers.WorkflowHandler" /> (publisher) from the
///     <see cref="WorkflowProgressObserver" /> (subscriber) without depending on
///     WorkflowCore-internal lifecycle hooks.
/// </summary>
public sealed class WorkflowEventBus
{
    /// <summary>
    ///     Raised whenever a workflow lifecycle event occurs (start, complete, suspend, resume,
    ///     error). Subscribers must be registered before the first workflow is started.
    /// </summary>
    public event Action<WorkflowProgress>? OnProgress;

    /// <summary>
    ///     Publishes a <see cref="WorkflowProgress" /> to all current subscribers.
    ///     Safe to call from any thread.
    /// </summary>
    /// <param name="progress">The progress notification payload to deliver.</param>
    public void Publish(WorkflowProgress progress)
    {
        OnProgress?.Invoke(progress);
    }
}