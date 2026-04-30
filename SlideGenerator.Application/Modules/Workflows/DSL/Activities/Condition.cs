namespace SlideGenerator.Application.Modules.Workflows.DSL.Activities;

/// <summary>
///     Conditionally executes one of two branches based on a predicate evaluated at runtime.
///     The predicate receives a typed <see cref="IExecutionContext{TData}" /> for direct <c>ctx.Data</c> access.
/// </summary>
/// <typeparam name="TData">The workflow data type.</typeparam>
public sealed class Condition<TData> : Activity<TData>
{
    public Condition() { }

    public Condition(Func<IExecutionContext<TData>, bool> predicate, Activity<TData> then, Activity<TData>? @else = null)
    {
        Predicate = predicate;
        Then = then;
        Else = @else;
    }

    public Func<IExecutionContext<TData>, bool> Predicate { get; init; } = default!;
    public Activity<TData> Then { get; init; } = default!;
    public Activity<TData>? Else { get; init;  }
    
    public override async Task ExecuteAsync(IExecutionContext<TData> context)
    {
        if (Predicate(context))
            await Then.ExecuteAsync(context).ConfigureAwait(false);
        else if (Else != null)
            await Else.ExecuteAsync(context).ConfigureAwait(false);
    }
}