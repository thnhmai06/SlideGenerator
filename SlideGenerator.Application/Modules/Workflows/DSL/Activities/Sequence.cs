namespace SlideGenerator.Application.Modules.Workflows.DSL.Activities;

/// <summary>Executes a list of child nodes sequentially, in order.</summary>
public class Sequence<TData> : Activity<TData>
{
    public Sequence() { }

    /// <param name="steps">The ordered list of nodes to execute.</param>
    public Sequence(IEnumerable<Activity<TData>> steps)
    {
        Steps = steps;
    }

    public IEnumerable<Activity<TData>> Steps { get; init; } = [];


    public override async Task ExecuteAsync(IExecutionContext<TData> context)
    {
        foreach (var step in Steps)
            await step.ExecuteAsync(context).ConfigureAwait(false);
    }
}