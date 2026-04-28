using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Workflows.Models.States;

/// <summary>
///     Represents the execution state of an activity.
/// </summary>
public class ActivityState(
    string activityId, 
    IExecutionContext executionContext, 
    IReadOnlyDictionary<string, ExecutionState>? activities = null)
    : ExecutionState(activityId, executionContext, activities);
