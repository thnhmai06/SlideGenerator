using SlideGenerator.Application.Workflows.DSL.Nodes;

namespace SlideGenerator.Application.Workflows.DSL;

/// <summary>
///     Defines a workflow as a pure C# node tree, with no dependency on any workflow engine.
///     Infrastructure translates this tree into a concrete execution engine.
/// </summary>
/// <typeparam name="TData">The workflow data type.</typeparam>
public interface IWorkflowDefinition<TData>
{
    /// <summary>Gets the unique workflow identifier used for registration and lookup.</summary>
    string Id { get; }

    /// <summary>Gets the workflow version for registration.</summary>
    int Version { get; }

    /// <summary>Builds and returns the root node of the workflow activity tree.</summary>
    WorkflowNode Build();
}
