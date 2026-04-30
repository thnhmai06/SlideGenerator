namespace SlideGenerator.Application.Modules.Workflows.DSL.Activities;

/// <summary>Abstract base for all workflow DSL node types.</summary>
public abstract class Activity<TData>
{
    public abstract Task ExecuteAsync(IExecutionContext<TData> context);
}

public static class ActivityExtensions
{
    public static Task ExecuteAsync<TData>(this Activity<TData> node, IExecutionContext<TData> context)
    {
        return node.ExecuteAsync(context);
    }
}