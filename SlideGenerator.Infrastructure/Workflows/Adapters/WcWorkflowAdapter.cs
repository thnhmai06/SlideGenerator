using SlideGenerator.Application.Modules.Workflows.DSL;
using WorkflowCore.Interface;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Bridges an <see cref="Application.Modules.Workflows.DSL.IWorkflow{TData}" /> to WorkflowCore's <see cref="WorkflowCore.Interface.IWorkflow{TData}" />
///     by building a single <see cref="WcInterpreterStep{TDef,TData}" /> step as the workflow body.
/// </summary>
public sealed class WcWorkflowAdapter<TDef, TData>(TDef def) : WorkflowCore.Interface.IWorkflow<TData>
    where TDef : Application.Modules.Workflows.DSL.IWorkflow<TData>
    where TData : class, new()
{
    public string Id => def.Id;
    public int Version => def.Version;

    public void Build(IWorkflowBuilder<TData> builder)
    {
        builder.StartWith<WcInterpreterStep<TDef, TData>>();
    }
}