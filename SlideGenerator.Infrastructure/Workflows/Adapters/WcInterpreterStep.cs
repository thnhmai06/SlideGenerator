using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.Models.States;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Single WorkflowCore step that interprets the entire <see cref="Application.Modules.Workflows.DSL.IWorkflow{TData}" /> node tree
///     in-process. Parallel branches use <c>Task.WhenAll</c>; ForEach iterations each run in a dedicated
///     child scope created via <see cref="IExecutionContext{TData}.CreateChildScope" />.
/// </summary>
public sealed class WcInterpreterStep<TDef, TData>(
    IServiceProvider services,
    WorkflowSnapshotRegistry snapshotRegistry) : StepBodyAsync
    where TDef : Application.Modules.Workflows.DSL.IWorkflow<TData>
    where TData : class
{
    private readonly TDef _def = (TDef)(services.GetService(typeof(TDef))
                                        ?? throw new InvalidOperationException(
                                            $"Workflow definition '{typeof(TDef).Name}' is not registered in DI."));

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (TData)context.Workflow.Data;
        var state = new WorkflowSnapshot(_def.Id, new WcExecutionPayload());
        var ctx = new WcInterpreterContext<TData>(data, state, context.CancellationToken, services);

        snapshotRegistry.Register(context.Workflow.Id, state);
        try
        {
            var workflow = _def.Build();
            await workflow.ExecuteAsync(ctx).ConfigureAwait(false);
        }
        finally
        {
            snapshotRegistry.Unregister(context.Workflow.Id);
            (data as IDisposable)?.Dispose();
        }

        return ExecutionResult.Next();
    }
}