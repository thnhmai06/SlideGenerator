namespace SlideGenerator.Application.Modules.Workflows.DSL.Nodes;

/// <summary>
///     Non-generic dispatch interface implemented by <see cref="ActivityNode{T}" />.
///     Allows the interpreter to invoke leaf activities without knowing the concrete type at compile time.
/// </summary>
public interface IActivityNode
{
    Task InvokeAsync(IActivityContext context);
}

/// <summary>
///     Represents a single leaf activity step in the workflow tree.
///     The activity is resolved from the DI container and executed with the current context.
/// </summary>
/// <typeparam name="T">The <see cref="ILeafActivity" /> implementation to invoke.</typeparam>
public record ActivityNode<T> : WorkflowNode, IActivityNode where T : ILeafActivity
{
    /// <inheritdoc />
    public Task InvokeAsync(IActivityContext context)
    {
        var activity = context.GetRequiredService<T>();
        return activity.ExecuteAsync(context);
    }
}
