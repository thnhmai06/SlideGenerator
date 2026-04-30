namespace SlideGenerator.Application.Modules.Workflows.DSL.Activities;

/// <summary>Executes all branches concurrently and waits for all to complete.</summary>
public class Parallel<TData> : Activity<TData>
{
    public Parallel() { }

    /// <param name="branches">The branches to run in parallel.</param>
    public Parallel(IEnumerable<Activity<TData>> branches)
    {
        Branches = branches.ToList();
    }

    public ICollection<Activity<TData>> Branches { get; init; } = [];

    public override async Task ExecuteAsync(IExecutionContext<TData> context)
    {
        await Task.WhenAll(Branches.Select(b => b.ExecuteAsync(context)))
            .ConfigureAwait(false);
    }
}