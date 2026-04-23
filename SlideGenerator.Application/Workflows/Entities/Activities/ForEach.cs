using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Application.Workflows.Interfaces;

namespace SlideGenerator.Application.Workflows.Entities.Activities;

/// <summary>
///     Describes sequential iteration over a collection of items.
///     The single <see cref="Body" /> activity is executed once per iteration.
///     Infrastructure provides a concrete executable form and writes <see cref="CurrentValue" />.Value before each run.
/// </summary>
/// <typeparam name="T">The element type of the iterated collection.</typeparam>
public abstract class ForEach<T> : Activity, IEnumerableActivity<T>
{
    /// <inheritdoc />
    public required ICollection<T> Items { get; init; }

    /// <summary>The body activity executed once per iteration.</summary>
    public required Activity Body { get; init; }

    /// <summary>
    ///     Variable written by the executor with the current item before each body run.
    ///     Activities inside <see cref="Body" /> read from this variable.
    /// </summary>
    public required Variable<T?> CurrentValue { get; init; }
}
