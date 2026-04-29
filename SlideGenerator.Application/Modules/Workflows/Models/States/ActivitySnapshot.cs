using ExecutionContext = SlideGenerator.Application.Modules.Workflows.Models.States.IExecutionContext;

namespace SlideGenerator.Application.Modules.Workflows.Models.States;

/// <summary>Represents the execution snapshot of an activity.</summary>
public class ActivitySnapshot(
    string activityId,
    IExecutionPayload payload,
    ExecutionContext? context = null,
    IReadOnlyDictionary<string, ExecutionSnapshot>? activities = null)
    : ExecutionSnapshot(activityId, payload, context, activities);