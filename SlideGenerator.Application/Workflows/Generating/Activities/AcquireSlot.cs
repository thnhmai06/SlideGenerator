using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using SlideGenerator.Application.Resources.Abstractions;
using SlideGenerator.Application.Workflows.Generating.Rules;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Acquires a slot from the process-wide <see cref="IAsyncKeyedLocker{TKey}" /> gate identified
///     by a <see cref="SlotType" /> and outputs a unique lease key for explicit release via
///     <see cref="ReleaseSlot" />.  A deferred task always releases the lease when the workflow ends,
///     acting as a safety net.
/// </summary>
public sealed class AcquireSlot(IAsyncKeyedLocker<SlotType> locker) : Activity
{
    /// <summary>Gets the type of gate to acquire a slot from.</summary>
    public required Input<SlotType> Gate { get; init; }

    /// <summary>Gets the output lease key that uniquely identifies the acquired slot for use with <see cref="ReleaseSlot" />.</summary>
    public Output<string> LeaseKey { get; init; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var slotType = context.Get(Gate);

        var lease = await locker
            .LockAsync(slotType, context.CancellationToken)
            .ConfigureAwait(false);

        var leaseKey = $"slot-lease:{Guid.NewGuid():N}";
        context.WorkflowExecutionContext.TransientProperties[leaseKey] = lease;
        context.Set(LeaseKey, leaseKey);

        context.WorkflowExecutionContext.DeferTask(() =>
        {
            ReleaseSlot.TryRelease(context.WorkflowExecutionContext, leaseKey);
            return Task.CompletedTask;
        });
    }
}
