using SlideGenerator.Application.Workflows.Entities.Activities;

namespace SlideGenerator.Application.Workflows.Interfaces;

public interface ICompositeActivity
{
    /// <summary>The body activity to be executed.</summary>
    Activity Body { get; init; }
}