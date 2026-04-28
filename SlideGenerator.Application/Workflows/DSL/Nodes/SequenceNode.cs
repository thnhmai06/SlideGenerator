namespace SlideGenerator.Application.Workflows.DSL.Nodes;

/// <summary>Executes a list of child nodes sequentially, in order.</summary>
/// <param name="Steps">The ordered list of nodes to execute.</param>
public record SequenceNode(IReadOnlyList<WorkflowNode> Steps) : WorkflowNode;
