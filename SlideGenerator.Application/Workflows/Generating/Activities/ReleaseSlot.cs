using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Releases a slot lease previously acquired by <see cref="AcquireSlot" /> using its output lease key.
/// </summary>
public sealed class ReleaseSlot : Activity
{
    /// <summary>Gets the lease key previously output by <see cref="AcquireSlot.LeaseKey" />.</summary>
    public required Input<string> LeaseKey { get; init; }

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        TryRelease(context.WorkflowExecutionContext, context.Get(LeaseKey)!);
        return ValueTask.CompletedTask;
    }

    internal static void TryRelease(WorkflowExecutionContext ctx, string leaseKey)
    {
        if (ctx.TransientProperties.TryGetValue(leaseKey, out var v) && v is IDisposable lease)
        {
            lease.Dispose();
            ctx.TransientProperties.Remove(leaseKey);
        }
    }
}
