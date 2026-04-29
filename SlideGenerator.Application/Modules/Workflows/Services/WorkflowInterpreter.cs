using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.DSL.Nodes;
using SlideGenerator.Application.Modules.Workflows.Models.Logging;

namespace SlideGenerator.Application.Modules.Workflows.Services;

/// <summary>
///     Core interpreter for the custom Workflow-as-Code DSL.
///     Traverses the <see cref="WorkflowNode" /> tree and executes each node in-process.
/// </summary>
public sealed class WorkflowInterpreter(IServiceProvider services, GateLocker gateLocker)
{
    /// <summary>
    ///     Recursively executes a <see cref="WorkflowNode" /> and its descendants.
    /// </summary>
    public async Task ExecuteAsync<TData>(WorkflowNode node, IActivityContext<TData> ctx)
    {
        switch (node)
        {
            case IActivityNode activity:
                await activity.InvokeAsync(ctx, services).ConfigureAwait(false);
                break;

            case SequenceNode seq:
                foreach (var step in seq.Steps)
                    await ExecuteAsync(step, ctx).ConfigureAwait(false);
                break;

            case IForEachNode fe:
                var setters = fe.ResolveSetters(ctx);
                if (fe.Parallel)
                    await Task.WhenAll(setters.Select(async setter =>
                    {
                        var child = ctx.CreateChildScope();
                        setter(child);
                        await ExecuteAsync(fe.Body, child).ConfigureAwait(false);
                    })).ConfigureAwait(false);
                else
                    foreach (var setter in setters)
                    {
                        var child = ctx.CreateChildScope();
                        setter(child);
                        await ExecuteAsync(fe.Body, child).ConfigureAwait(false);
                    }

                break;

            case ParallelNode par:
                await Task.WhenAll(par.Branches.Select(b => ExecuteAsync(b, ctx)))
                    .ConfigureAwait(false);
                break;

            case TryNode tryNode:
                try
                {
                    await ExecuteAsync(tryNode.Body, ctx).ConfigureAwait(false);
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
                        await ExecuteAsync(tryNode.Catch, catchCtx).ConfigureAwait(false);
                    }
                }

                break;

            case GateNode gn:
                ctx.State.Logger.AddLog(LogLevel.Info, $"Waiting for gate: {gn.Gate}...");
                using (await gateLocker.LockAsync(gn.Gate, ctx.CancellationToken).ConfigureAwait(false))
                {
                    ctx.State.Logger.AddLog(LogLevel.Info, $"Acquired gate: {gn.Gate}. Executing body...");
                    await ExecuteAsync(gn.Body, ctx).ConfigureAwait(false);
                    ctx.State.Logger.AddLog(LogLevel.Info, $"Releasing gate: {gn.Gate}.");
                }

                break;

            case IInlineNode inline:
                await inline.ExecuteAsync(ctx).ConfigureAwait(false);
                break;

            case IConditionNode ifNode:
                if (ifNode.EvaluatePredicate(ctx))
                    await ExecuteAsync(ifNode.Then, ctx).ConfigureAwait(false);
                else if (ifNode.Else != null)
                    await ExecuteAsync(ifNode.Else, ctx).ConfigureAwait(false);
                break;
        }
    }
}
