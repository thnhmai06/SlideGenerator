using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Application.Workflows.Interfaces;

namespace SlideGenerator.Application.Workflows.Entities.Activities;

/// <summary>
///     Sequentially iterates over a collection of items.
/// </summary>
/// <remarks>
///     The single <see cref="Body" /> activity is executed once per iteration.
///     Infrastructure provides a concrete executable form and writes <see cref="CurrentValue" />.Value before each run.
/// </remarks>
/// <typeparam name="T">The element type of the iterated collection.</typeparam>
public abstract class ForEach<T> : Activity, IEnumerableActivity<T>
{
    /// <inheritdoc />
    public required ICollection<T> Items { get; init; }

    /// <summary>The body activity executed once per iteration.</summary>
    public required Activity Body { get; init; }

    /// <summary>
    ///     Variable written by the executor with the current item before each body run.
    /// </summary>
    /// <remarks>
    ///     Activities inside <see cref="Body" /> read from this variable.
    /// </remarks>
    public required Variable<T?> CurrentValue { get; init; }
}
