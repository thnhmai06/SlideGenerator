using Elsa.Workflows;
using SlideGenerator.Application.Workflows.Generating.Services;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Provides shared logic for releasing a flow-scoped activity permit.
/// </summary>
public abstract class ReleaseActivitySlot : Activity
{
    /// <summary>
    ///     Gets the transient property key used to locate the stored lease in workflow execution context.
    /// </summary>
    protected abstract string LeaseKey { get; }

    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        ReleaseLease(context.WorkflowExecutionContext, LeaseKey);
        return ValueTask.CompletedTask;
    }

    internal static void ReleaseLease(WorkflowExecutionContext workflowExecutionContext, string leaseKey)
    {
        if (!workflowExecutionContext.TransientProperties.TryGetValue(leaseKey, out var leaseValue))
            return;

        if (leaseValue is not ActivityLease lease)
            return;

        lease.Dispose();
        _ = workflowExecutionContext.TransientProperties.Remove(leaseKey);
    }
}