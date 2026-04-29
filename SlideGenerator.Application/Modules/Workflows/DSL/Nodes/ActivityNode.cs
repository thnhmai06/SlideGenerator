namespace SlideGenerator.Application.Modules.Workflows.DSL.Nodes;

/// <summary>
///     Non-generic dispatch interface implemented by <see cref="ActivityNode{T}" />.
///     The interpreter passes its own <see cref="IServiceProvider" /> so that activities are
///     resolved from DI rather than pulled through the activity context.
/// </summary>
public interface IActivityNode
{
    Task InvokeAsync(IActivityContext context, IServiceProvider services);
}

/// <summary>
///     Represents a single leaf activity step in the workflow tree.
///     The activity is resolved from the supplied DI container and executed with the current context.
/// </summary>
/// <typeparam name="T">The <see cref="ILeafActivity" /> implementation to invoke.</typeparam>
public record ActivityNode<T> : WorkflowNode, IActivityNode where T : ILeafActivity
{
    /// <inheritdoc />
    public Task InvokeAsync(IActivityContext context, IServiceProvider services)
    {
        var activity = (T)(services.GetService(typeof(T))
                           ?? throw new InvalidOperationException(
                               $"Activity '{typeof(T).Name}' is not registered in the DI container."));
        return activity.ExecuteAsync(context);
    }
}
