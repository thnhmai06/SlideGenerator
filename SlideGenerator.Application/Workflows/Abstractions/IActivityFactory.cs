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
    /// <param name="activities">The collection of activities to execute in order.</param>
    /// <param name="name">Optional display name for the sequence activity.</param>
    /// <returns>A new <see cref="Sequence" /> activity.</returns>
    Sequence Sequence(ICollection<Activity> activities, string? name = null);

    /// <summary>Creates a leaf activity that executes <paramref name="function" /> inline.</summary>
    /// <param name="function">The asynchronous function to execute.</param>
    /// <param name="name">Optional display name for the inline activity.</param>
    /// <returns>A new <see cref="Inline" /> activity.</returns>
    Inline Inline(Func<IExecutionContext, CancellationToken, ValueTask> function, string? name = null);

    /// <summary>Creates a sequential loop over <paramref name="items" />, writing each into <paramref name="container" />.</summary>
    /// <typeparam name="T">The type of items to iterate over.</typeparam>
    /// <param name="items">The collection of items to iterate.</param>
    /// <param name="container">The variable to store the current item in each iteration.</param>
    /// <param name="body">The activity to execute for each item.</param>
    /// <param name="name">Optional display name for the loop activity.</param>
    /// <returns>A new <see cref="ForEach{T}" /> activity.</returns>
    ForEach<T> ForEach<T>(IEnumerable<T> items, Variable<T?> container, Activity body, string? name = null)
        where T : notnull;

    /// <summary>Creates a parallel loop over <paramref name="items" />, writing each into <paramref name="container" />.</summary>
    /// <typeparam name="T">The type of items to iterate over.</typeparam>
    /// <param name="items">The collection of items to iterate.</param>
    /// <param name="container">The variable to store the current item in each iteration.</param>
    /// <param name="body">The activity to execute for each item.</param>
    /// <param name="name">Optional display name for the parallel loop activity.</param>
    /// <returns>A new <see cref="ParallelForEach{T}" /> activity.</returns>
    ParallelForEach<T> ParallelForEach<T>(IEnumerable<T> items, Variable<T?> container, Activity body,
        string? name = null) where T : notnull;

    /// <summary>Creates an activity that acquires a concurrency slot before running <paramref name="body" />.</summary>
    /// <param name="gate">The type of concurrency gate to use.</param>
    /// <param name="body">The activity to execute within the gate.</param>
    /// <param name="name">Optional display name for the gated activity.</param>
    /// <returns>A new <see cref="SlotGated" /> activity.</returns>
    SlotGated SlotGated(SlotType gate, Activity body, string? name = null);
}
