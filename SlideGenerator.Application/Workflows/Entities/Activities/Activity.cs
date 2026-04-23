
using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Workflows.Entities.Activities;

/// <summary>
///     Base class for all Application-layer workflow activities. Carries only a display name;
///     concrete subtypes supply their own execution contract.
/// </summary>
public abstract class Activity : Entry
{
    /// <remarks>
    ///     Composite activities (<see cref="Sequence" />, <see cref="ForEach{T}" />, etc.) are handled
    ///     structurally by the executor and never reach this method. Override in leaf activities.
    /// </remarks>
    public abstract ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default);
}
