using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Abstractions;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Infrastructure.Workflows.Adapters;

namespace SlideGenerator.Infrastructure.Workflows.Services;

/// <summary>
///     Infrastructure implementation of <see cref="IActivityFactory" /> that creates Elsa-compatible workflow activities.
/// </summary>
public sealed class ElsaActivityFactory : IActivityFactory
{
    /// <inheritdoc />
    public Sequence Sequence(ICollection<Activity> activities, string? name = null) 
        => new ElsaSequence { Name = name, Activities = activities };

    /// <inheritdoc />
    public Inline Inline(Func<IExecutionContext, CancellationToken, ValueTask> function, string? name = null) 
        => new ElsaInline { Name = name, Function = function };

    /// <inheritdoc />
    public ForEach<T> ForEach<T>(
        IEnumerable<T> items, Variable<T?> container, Activity body,
        string? name = null) where T : notnull 
        => new ElsaForEach<T> { Name = name, CurrentValue = container, Items = items.ToList(), Body = body };

    /// <inheritdoc />
    public ParallelForEach<T> ParallelForEach<T>(IEnumerable<T> items,
        Variable<T?> container,
        Activity body, string? name = null) where T : notnull 
        => new ElsaParallelForEach<T> { Name = name, CurrentValue = container, Items = items.ToList(), Body = body };

    /// <inheritdoc />
    public SlotGated SlotGated(SlotType gate, Activity body, string? name = null) 
        => new ElsaSlotGated { Name = name, Gate = gate, Body = body };
}
