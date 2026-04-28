namespace SlideGenerator.Application.Modules.Workflows.DSL.Nodes;

/// <summary>
///     Non-generic dispatch interface implemented by <see cref="InlineNode{TData}" />.
///     Allows the interpreter to invoke the inline action without knowing the data type.
/// </summary>
public interface IInlineNode
{
    /// <summary>Executes the inline action with the provided context.</summary>
    Task ExecuteAsync(IActivityContext ctx);
}

/// <summary>
///     Executes an inline delegate as a workflow step.
///     Use for lightweight state mutations that do not warrant a dedicated activity class.
///     The lambda receives a typed <see cref="IActivityContext{TData}" /> for direct <c>ctx.Data</c> access.
/// </summary>
/// <typeparam name="TData">The workflow data type.</typeparam>
public record InlineNode<TData>(Func<IActivityContext<TData>, Task> Action) : WorkflowNode, IInlineNode
{
    Task IInlineNode.ExecuteAsync(IActivityContext ctx) => Action((IActivityContext<TData>)ctx);
}
