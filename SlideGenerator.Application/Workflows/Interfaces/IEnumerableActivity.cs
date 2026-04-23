using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Workflows.Interfaces;

/// <summary>
///     Defines a contract for iteration activities.
/// </summary>
/// <remarks>
///     The executor reads <see cref="Items" />, writes <see cref="CurrentValue" />.Value before each iteration,
///     and executes <see cref="ICompositeActivity.Body" />.
/// </remarks>
/// <typeparam name="T">The element type of the iterated collection.</typeparam>
public interface IEnumerableActivity<T> : ICompositeActivity
{
    /// <summary>Gets the collection of items to iterate.</summary>
    ICollection<T> Items { get; init; }

    /// <summary>Gets the variable the executor populates with the current item before each body run.</summary>
    Variable<T?> CurrentValue { get; }
}
