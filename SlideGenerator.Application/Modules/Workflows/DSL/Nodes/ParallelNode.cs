namespace SlideGenerator.Application.Modules.Workflows.DSL.Nodes;

/// <summary>Executes all branches concurrently and waits for all to complete.</summary>
/// <param name="Branches">The branches to run in parallel.</param>
public record ParallelNode(IReadOnlyList<WorkflowNode> Branches) : WorkflowNode;
