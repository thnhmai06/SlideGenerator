using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Interfaces;

namespace SlideGenerator.Application.Workflows.Entities.Activities;

/// <summary>
///     Describes a body activity wrapped with a concurrency gate.
///     Infrastructure acquires a slot before running <see cref="Body" /> and releases it afterward.
/// </summary>
public abstract class SlotGated : Activity, ICompositeActivity
{
    /// <summary>Gets the concurrency gate to acquire before executing <see cref="Body" />.</summary>
    public required SlotType Gate { get; init; }

    /// <summary>Gets the activity to execute inside the concurrency gate.</summary>
    public required Activity Body { get; init; }
}
