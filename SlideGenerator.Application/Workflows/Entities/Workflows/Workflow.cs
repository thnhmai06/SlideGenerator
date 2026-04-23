using SlideGenerator.Application.Workflows.Entities.Activities;

namespace SlideGenerator.Application.Workflows.Entities.Workflows;

/// <summary>
///     Base class for Application-layer workflow definitions.
///     A workflow is a named, composable unit that builds an <see cref="Activity" /> tree
///     from its typed input. Infrastructure converts this tree and executes it via the
///     underlying workflow engine.
/// </summary>
public abstract class Workflow<TInput> : Entry
{
    public abstract Activity Build(TInput input);
}
