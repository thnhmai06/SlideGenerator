using SlideGenerator.Application.Services.Generating.Rules;

namespace SlideGenerator.Application.Modules.Workflows.DSL.Nodes;

/// <summary>
///     Acquires a named concurrency slot before executing the body and releases it afterward.
///     Throttles concurrent executions across parallel branches.
/// </summary>
/// <param name="Gate">The slot type that controls the concurrency limit.</param>
/// <param name="Body">The node to execute while the slot is held.</param>
public record SlotGatedNode(SlotType Gate, WorkflowNode Body) : WorkflowNode;
