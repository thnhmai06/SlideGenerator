using SlideGenerator.Application.Modules.Workflows.Entities.Contexts;
using SlideGenerator.Application.Modules.Workflows.Models.States;

namespace SlideGenerator.Application.Services.Generating.Models.States;

/// <summary>
///     Represents the state of a worksheet processing activity.
/// </summary>
public class WorksheetState(
    string activityId,
    IExecutionContext executionContext,
    IReadOnlyDictionary<string, ExecutionState>? activities = null,
    IReadOnlyDictionary<int, string>? rowActivityMap = null)
    : ActivityState(activityId, executionContext, activities)
{
    /// <summary>Gets the mapping from row indices to activity IDs.</summary>
    public IReadOnlyDictionary<int, string> RowIds { get; } = rowActivityMap ?? new Dictionary<int, string>();

    /// <summary>Retrieves the state of a specific row activity by its index.</summary>
    /// <param name="rowIndex">The index of the row.</param>
    /// <returns>The state if found; otherwise, <see langword="null" />.</returns>
    public ActivityState? GetRow(int rowIndex) =>
        RowIds.TryGetValue(rowIndex, out var id) ? GetActivity<ActivityState>(id) : null;
}