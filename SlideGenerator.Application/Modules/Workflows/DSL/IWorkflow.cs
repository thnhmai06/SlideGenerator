using SlideGenerator.Application.Modules.Workflows.DSL.Activities;

namespace SlideGenerator.Application.Modules.Workflows.DSL;

/// <summary>
///     Defines a workflow as a pure C# node tree, with no dependency on any workflow engine.
///     Infrastructure translates this tree into a concrete execution engine.
/// </summary>
/// <typeparam name="TData">The workflow data type.</typeparam>
public interface IWorkflow<TData>
{
    /// <summary>Gets the unique workflow identifier used for registration and lookup.</summary>
    string Id { get; }

    /// <summary>Gets the workflow version for registration.</summary>
    int Version { get; }

    /// <summary>Builds and returns the root node of the workflow activity tree.</summary>
    Activity<TData> Build();
}