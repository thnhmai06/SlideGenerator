using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Application.Workflows.Models.States;

namespace SlideGenerator.Application.Services.Generating.Models.States;

/// <summary>
///     Represents the state of a workbook processing activity.
/// </summary>
public class WorkbookState(
    string activityId,
    IExecutionContext executionContext,
    IReadOnlyDictionary<string, ExecutionState>? activities = null,
    IReadOnlyDictionary<string, string>? worksheetActivityMap = null)
    : ActivityState(activityId, executionContext, activities)
{
    /// <summary>Gets the mapping from worksheet names to activity IDs.</summary>
    public IReadOnlyDictionary<string, string> WorksheetIds { get; } =
        worksheetActivityMap ?? new Dictionary<string, string>();

    /// <summary>Retrieves the state of a specific worksheet activity by its name.</summary>
    /// <param name="worksheetName">The name of the worksheet.</param>
    /// <returns>The state if found; otherwise, <see langword="null" />.</returns>
    public WorksheetState? GetWorksheet(string worksheetName) =>
        WorksheetIds.TryGetValue(worksheetName, out var id) ? GetActivity<WorksheetState>(id) : null;
}