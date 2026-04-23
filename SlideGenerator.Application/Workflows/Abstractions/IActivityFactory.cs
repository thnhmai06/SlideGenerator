using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Workflows.Abstractions;

/// <summary>
///     Creates concrete workflow activity instances.
///     Application layer depends on this abstraction; Infrastructure implements it using Elsa.
/// </summary>
public interface IActivityFactory
{
    /// <summary>Creates a sequential activity that runs <paramref name="activities" /> one after another.</summary>
    Sequence Sequence(ICollection<Activity> activities, string? name = null);

    /// <summary>Creates a leaf activity that executes <paramref name="function" /> inline.</summary>
    Inline Inline(Func<IExecutionContext, CancellationToken, ValueTask> function, string? name = null);


    /// <summary>Creates a sequential loop over <paramref name="items" />, writing each into <paramref name="container" />.</summary>
    ForEach<T> ForEach<T>(IEnumerable<T> items, Variable<T?> container, Activity body, string? name = null) 
        where T : notnull;

    /// <summary>Creates a parallel loop over <paramref name="items" />, writing each into <paramref name="container" />.</summary>
    ParallelForEach<T> ParallelForEach<T>(IEnumerable<T> items, Variable<T?> container, Activity body,
        string? name = null)  where T : notnull;

    /// <summary>Creates an activity that acquires a concurrency slot before running <paramref name="body" />.</summary>
    SlotGated SlotGated(SlotType gate, Activity body, string? name = null);
}
