using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Interfaces;

namespace SlideGenerator.Application.Workflows.Entities.Activities;

/// <summary>
///     Executes an activity within a concurrency gate.
/// </summary>
/// <remarks>
///     Infrastructure acquires a slot before running <see cref="Body" /> and releases it afterward.
/// </remarks>
public abstract class SlotGated : Activity, ICompositeActivity
{
    /// <summary>Gets the concurrency gate to acquire before executing <see cref="Body" />.</summary>
    public required SlotType Gate { get; init; }

    /// <summary>Gets the activity to execute inside the concurrency gate.</summary>
    public required Activity Body { get; init; }
}