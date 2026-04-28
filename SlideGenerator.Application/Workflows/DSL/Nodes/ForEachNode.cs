namespace SlideGenerator.Application.Workflows.DSL.Nodes;

/// <summary>
///     Non-generic dispatch interface implemented by <see cref="ForEachNode{TItem,TData}" />.
///     Allows the interpreter to drive iteration without knowing the concrete item or data type.
///     Item values are captured inside typed closures returned by <see cref="ResolveSetters" />
///     — no <c>object</c> crosses the interface boundary.
/// </summary>
public interface IForEachNode
{
    /// <summary>Gets a value indicating whether iterations run in parallel.</summary>
    bool Parallel { get; }

    /// <summary>Gets the node to execute for each item.</summary>
    WorkflowNode Body { get; }

    /// <summary>
    ///     Returns one setter per iteration item. Each setter, when called with a child scope,
    ///     writes the typed item into that scope via <see cref="IActivityContext.SetVariable{TVar}" />.
    ///     Item values are captured in closures so no <c>object</c> is exposed here.
    /// </summary>
    IReadOnlyList<Action<IActivityContext>> ResolveSetters(IActivityContext ctx);
}

/// <summary>
///     Iterates over a collection of items, executing the body node for each in an isolated child scope.
///     The <see cref="Items" /> delegate receives the parent context, allowing access to ancestor
///     variables (e.g., reading the outer loop's current worksheet when building the inner row list).
/// </summary>
/// <typeparam name="TItem">The element type of the iterated collection.</typeparam>
/// <typeparam name="TData">The workflow data type.</typeparam>
public record ForEachNode<TItem, TData>(
    Variable<TItem> Variable,
    Func<IActivityContext<TData>, IEnumerable<TItem>> Items,
    bool Parallel,
    WorkflowNode Body) : WorkflowNode, IForEachNode
{
    /// <summary>Lists the typed items for this loop using the current parent context.</summary>
    public IEnumerable<TItem> GetItems(IActivityContext<TData> ctx) => Items(ctx);

    IReadOnlyList<Action<IActivityContext>> IForEachNode.ResolveSetters(IActivityContext ctx) =>
        Items((IActivityContext<TData>)ctx)
            .Select<TItem, Action<IActivityContext>>(item => child => child.SetVariable(Variable, item))
            .ToList();
}
