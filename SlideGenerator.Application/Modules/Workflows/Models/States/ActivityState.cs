using SlideGenerator.Application.Modules.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Modules.Workflows.Models.States;

/// <summary>
///     Represents the execution state of an activity.
/// </summary>
public class ActivityState(
    string activityId,
    IExecutionContext executionContext,
    IReadOnlyDictionary<string, ExecutionState>? activities = null)
    : ExecutionState(activityId, executionContext, activities);