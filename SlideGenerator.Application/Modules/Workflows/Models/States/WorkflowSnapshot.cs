using ExecutionContext = SlideGenerator.Application.Modules.Workflows.Models.States.IExecutionContext;

namespace SlideGenerator.Application.Modules.Workflows.Models.States;

/// <summary>Represents the execution snapshot of a workflow instance.</summary>
public class WorkflowSnapshot(
    string workflowId,
    IExecutionPayload payload,
    ExecutionContext? executionContext = null,
    IReadOnlyDictionary<string, ExecutionSnapshot>? activities = null)
    : ExecutionSnapshot(workflowId, payload, executionContext, activities);