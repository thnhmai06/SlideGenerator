using SlideGenerator.Application.Services.Generating.Rules;

namespace SlideGenerator.Application.Modules.Workflows.DSL.Nodes;

/// <summary>
///     Acquires a named concurrency gate before executing the body and releases it afterward.
///     Throttles concurrent executions across parallel branches.
/// </summary>
/// <param name="Gate">The gate type that controls the concurrency limit.</param>
/// <param name="Body">The node to execute while the gate is held.</param>
public record GateNode(GateType Gate, WorkflowNode Body) : WorkflowNode;