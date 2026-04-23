using SlideGenerator.Application.Workflows.Entities.Activities;

namespace SlideGenerator.Application.Workflows.Interfaces;

/// <summary>
///     Defines a workflow activity that contains and executes a body activity.
/// </summary>
public interface ICompositeActivity
{
    /// <summary>The body activity to be executed.</summary>
    Activity Body { get; init; }
}