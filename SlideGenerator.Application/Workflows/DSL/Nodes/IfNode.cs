namespace SlideGenerator.Application.Workflows.DSL.Nodes;

/// <summary>
///     Non-generic dispatch interface implemented by <see cref="ConditionNode{TData}" />.
///     Allows the interpreter to evaluate the predicate and access branches without knowing the data type.
/// </summary>
public interface IConditionNode
{
    /// <summary>Evaluates the predicate using the provided context.</summary>
    bool EvaluatePredicate(IActivityContext ctx);

    /// <summary>Gets the node to execute when the predicate is <see langword="true" />.</summary>
    WorkflowNode Then { get; }

    /// <summary>Gets the optional node to execute when the predicate is <see langword="false" />.</summary>
    WorkflowNode? Else { get; }
}

/// <summary>
///     Conditionally executes one of two branches based on a predicate evaluated at runtime.
///     The predicate receives a typed <see cref="IActivityContext{TData}" /> for direct <c>ctx.Data</c> access.
/// </summary>
/// <typeparam name="TData">The workflow data type.</typeparam>
public record ConditionNode<TData>(
    Func<IActivityContext<TData>, bool> Predicate,
    WorkflowNode Then,
    WorkflowNode? Else = null) : WorkflowNode, IConditionNode
{
    bool IConditionNode.EvaluatePredicate(IActivityContext ctx) => Predicate((IActivityContext<TData>)ctx);
}
