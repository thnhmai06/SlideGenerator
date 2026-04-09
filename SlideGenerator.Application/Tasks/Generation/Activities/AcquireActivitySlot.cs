using System.Collections.Concurrent;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using SlideGenerator.Application.Tasks.Generation.Services;
using SlideGenerator.Domain.Settings.Entities;
using SlideGenerator.Domain.Settings.Interfaces;

namespace SlideGenerator.Application.Tasks.Generation.Activities;

/// <summary>
///     Provides shared logic for acquiring a flow-scoped activity permit.
/// </summary>
public abstract class AcquireActivitySlot(ISettingProvider settingProvider) : Activity
{
    private static readonly ConcurrentDictionary<string, ActivityGate> Gates = new(StringComparer.Ordinal);

    /// <summary>
    ///     Gets a unique key that identifies the process-wide gate for this flow.
    /// </summary>
    protected abstract string GateKey { get; }

    /// <summary>
    ///     Gets the transient property key used to store the lease in workflow execution context.
    /// </summary>
    protected abstract string LeaseKey { get; }

    /// <summary>
    ///     Resolves configured concurrency for this flow from the current settings.
    /// </summary>
    /// <param name="setting">The current application setting instance.</param>
    /// <returns>The configured concurrency value for the flow.</returns>
    protected abstract int GetConfiguredConcurrency(Setting setting);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var configuredConcurrency = GetConfiguredConcurrency(settingProvider.Current);
        var normalizedConcurrency = configuredConcurrency > 0
            ? configuredConcurrency
            : ActivityGate.DefaultMaximumConcurrency;

        var gate = Gates.GetOrAdd(GateKey, _ => new ActivityGate(normalizedConcurrency));
        var lease = await gate.AcquireAsync(context.CancellationToken).ConfigureAwait(false);

        var workflowExecutionContext = context.WorkflowExecutionContext;
        workflowExecutionContext.TransientProperties[LeaseKey] = lease;

        workflowExecutionContext.DeferTask(() =>
        {
            ReleaseActivitySlot.ReleaseLease(workflowExecutionContext, LeaseKey);
            return Task.CompletedTask;
        });
    }
}