using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Workflows.Entities.Activities;

/// <summary>
///     Base class for all Application-layer workflow activities. Carries only a display name;
///     concrete subtypes supply their own execution contract.
/// </summary>
public abstract class Activity : Entry
{
    /// <summary>Executes the activity asynchronously.</summary>
    /// <param name="context">The execution context providing access to workflow state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    /// <remarks>
    ///     Composite activities (<see cref="Sequence" />, <see cref="ForEach{T}" />, etc.) are handled
    ///     structurally by the executor and never reach this method. Override in leaf activities.
    /// </remarks>
    public abstract ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default);
}