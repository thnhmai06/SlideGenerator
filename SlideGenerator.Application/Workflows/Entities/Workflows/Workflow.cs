using SlideGenerator.Application.Workflows.Entities.Activities;

namespace SlideGenerator.Application.Workflows.Entities.Workflows;

/// <summary>
///     Base class for Application-layer workflow definitions.
///     A workflow is a named, composable unit that builds an <see cref="Activity" /> tree
///     from its typed input. Infrastructure converts this tree and executes it via the
///     underlying workflow engine.
/// </summary>
/// <typeparam name="TInput">The type of input required to build the workflow.</typeparam>
public abstract class Workflow<TInput> : Entry
{
    /// <summary>Builds the activity tree for the workflow using the provided input.</summary>
    /// <param name="input">The input data for the workflow.</param>
    /// <returns>An <see cref="Activity" /> representing the root of the workflow's activity tree.</returns>
    public abstract Activity Build(TInput input);
}