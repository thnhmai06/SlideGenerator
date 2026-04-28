using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Workflows.Models.States;

/// <summary>
///     Represents the execution state of a workflow instance.
/// </summary>
public class WorkflowState(
    string workflowId, 
    IExecutionContext executionContext, 
    IReadOnlyDictionary<string, ExecutionState>? activities = null)
    : ExecutionState(workflowId, executionContext, activities);
