using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Workflows.Models.Logging;

namespace SlideGenerator.Application.Modules.Workflows.DSL.Activities;

/// <summary>
///     Acquires a named concurrency gate before executing the body and releases it afterward.
///     Throttles concurrent executions across parallel branches.
/// </summary>
public sealed class GateWrapper<TGate, TData>(GateLocker<TGate> gateLocker) : Activity<TData> where TGate : notnull
{
    /// The gate type that controls the concurrency limit.
    public required TGate Gate { get; init; }
    
    /// The node to execute while the gate is held.
    public required Activity<TData> Body { get; init; }

    public override async Task ExecuteAsync(IExecutionContext<TData> context)
    {
        context.Snapshot.Logger.AddLog(LogLevel.Info, $"Waiting for gate: {Gate}...");
        using (await gateLocker.LockAsync(Gate, context.CancellationToken).ConfigureAwait(false))
        {
            context.Snapshot.Logger.AddLog(LogLevel.Info, $"Acquired gate: {Gate}. Executing body...");
            await Body.ExecuteAsync(context).ConfigureAwait(false);
            context.Snapshot.Logger.AddLog(LogLevel.Info, $"Releasing gate: {Gate}.");
        }
    }
}