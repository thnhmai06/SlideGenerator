using SlideGenerator.Application.Modules.Resources.Abstractions;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.DSL.Nodes;
using SlideGenerator.Application.Modules.Workflows.Models.Logging;
using SlideGenerator.Application.Modules.Workflows.Models.States;
using SlideGenerator.Application.Services.Generating.Rules;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Single WorkflowCore step that interprets the entire <see cref="IWorkflowDefinition{TData}" /> node tree
///     in-process. Parallel branches use <c>Task.WhenAll</c>; ForEach iterations each run in a dedicated
///     child scope created via <see cref="IActivityContext{TData}.CreateChildScope" />.
/// </summary>
public sealed class WcInterpreterStep<TDef, TData>(
    IServiceProvider services,
    WorkflowStateRegistry stateRegistry) : StepBodyAsync
    where TDef : IWorkflowDefinition<TData>, new()
    where TData : class
{
    private static readonly TDef Def = new();

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (TData)context.Workflow.Data;
        var state = new WorkflowState(Def.Id, new WcExecutionContext(services));
        var ctx = new WcInterpreterContext<TData>(data, state, services, context.CancellationToken);

        stateRegistry.Register(context.Workflow.Id, state);
        try
        {
            await ExecuteNodeAsync(Def.Build(), ctx).ConfigureAwait(false);
        }
        finally
        {
            stateRegistry.Unregister(context.Workflow.Id);
        }

        return ExecutionResult.Next();
    }

    private static async Task ExecuteNodeAsync(WorkflowNode node, IActivityContext<TData> ctx)
    {
        switch (node)
        {
            case IActivityNode activity:
                await activity.InvokeAsync(ctx).ConfigureAwait(false);
                break;

            case SequenceNode seq:
                foreach (var step in seq.Steps)
                    await ExecuteNodeAsync(step, ctx).ConfigureAwait(false);
                break;

            case IForEachNode fe:
                var setters = fe.ResolveSetters(ctx);
                if (fe.Parallel)
                    await Task.WhenAll(setters.Select(async setter =>
                    {
                        var child = ctx.CreateChildScope();
                        setter(child);
                        await ExecuteNodeAsync(fe.Body, child).ConfigureAwait(false);
                    })).ConfigureAwait(false);
                else
                    foreach (var setter in setters)
                    {
                        var child = ctx.CreateChildScope();
                        setter(child);
                        await ExecuteNodeAsync(fe.Body, child).ConfigureAwait(false);
                    }
                break;

            case ParallelNode par:
                await Task.WhenAll(par.Branches.Select(b => ExecuteNodeAsync(b, ctx)))
                    .ConfigureAwait(false);
                break;

            case TryNode tryNode:
                try
                {
                    await ExecuteNodeAsync(tryNode.Body, ctx).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ctx.State.Logger.AddLog(LogLevel.Error,
                        $"Row failed: {ex.GetType().Name}: {ex.Message}");
                    if (tryNode.Catch is not null)
                    {
                        var catchCtx = ctx.CreateChildScope();
                        if (tryNode.ExceptionVar is not null)
                            catchCtx.SetVariable(tryNode.ExceptionVar, ex);
                        await ExecuteNodeAsync(tryNode.Catch, catchCtx).ConfigureAwait(false);
                    }
                }
                break;

            case SlotGatedNode sg:
                var locker = ctx.GetRequiredService<IAsyncKeyedLocker<SlotType>>();
                using (await locker.LockAsync(sg.Gate, ctx.CancellationToken).ConfigureAwait(false))
                    await ExecuteNodeAsync(sg.Body, ctx).ConfigureAwait(false);
                break;

            case IInlineNode inline:
                await inline.ExecuteAsync(ctx).ConfigureAwait(false);
                break;

            case IConditionNode ifNode:
                if (ifNode.EvaluatePredicate(ctx))
                    await ExecuteNodeAsync(ifNode.Then, ctx).ConfigureAwait(false);
                else if (ifNode.Else != null)
                    await ExecuteNodeAsync(ifNode.Else, ctx).ConfigureAwait(false);
                break;
        }
    }
}
