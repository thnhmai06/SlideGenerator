using SlideGenerator.Application.Modules.Workflows.DSL;
using WorkflowCore.Interface;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Bridges an <see cref="IWorkflowDefinition{TData}" /> to WorkflowCore's <see cref="IWorkflow{TData}" />
///     by building a single <see cref="WcInterpreterStep{TDef,TData}" /> step as the workflow body.
/// </summary>
public sealed class WcWorkflowAdapter<TDef, TData> : IWorkflow<TData>
    where TDef : IWorkflowDefinition<TData>, new()
    where TData : class, new()
{
    private static readonly TDef Def = new();

    public string Id => Def.Id;
    public int Version => Def.Version;

    public void Build(IWorkflowBuilder<TData> builder)
    {
        builder.StartWith<WcInterpreterStep<TDef, TData>>();
    }
}