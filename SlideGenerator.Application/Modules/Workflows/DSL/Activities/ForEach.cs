namespace SlideGenerator.Application.Modules.Workflows.DSL.Activities;

/// <summary>
///     Iterates over a collection of items, executing the body node for each in an isolated child scope.
///     The <see cref="Items" /> delegate receives the parent context, allowing access to ancestor
///     variables (e.g., reading the outer loop's current worksheet when building the inner row list).
/// </summary>
/// <typeparam name="TItem">The element type of the iterated collection.</typeparam>
/// <typeparam name="TData">The workflow data type.</typeparam>
public sealed class ForEach<TItem, TData>(bool parallel) : Activity<TData>
{
    public ForEach(Func<IExecutionContext<TData>, IEnumerable<TItem>> items, Handle<TItem> handle, bool parallel, Activity<TData> body) : this(parallel)
    {
        Items = items;
        Handle = handle;
        Body = body;
    }

    public Func<IExecutionContext<TData>, IEnumerable<TItem>> Items { get; init; } = default!;
    public Handle<TItem> Handle { get; init; } = default!;
    public readonly bool Parallel = parallel;
    public Activity<TData> Body { get; init; } = default!;

    public override async Task ExecuteAsync(IExecutionContext<TData> context)
    {
        var items = Items(context);   
        
        if (Parallel)
            await Task.WhenAll(items.Select(async itemFactory =>
            {
                var childScope = context.CreateChildScope();
                childScope.SetVariable(Handle, itemFactory);
                await ExecuteAsync(childScope).ConfigureAwait(false);
            })).ConfigureAwait(false);
        else
            foreach (var item in items)
            {
                var childScope = context.CreateChildScope();
                childScope.SetVariable(Handle, item);
                await ExecuteAsync(childScope).ConfigureAwait(false);
            }
    }
}