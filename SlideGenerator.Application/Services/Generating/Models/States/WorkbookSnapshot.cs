using ExecutionContext = SlideGenerator.Application.Modules.Workflows.Models.States.IExecutionContext;
using SlideGenerator.Application.Modules.Workflows.Models.States;

namespace SlideGenerator.Application.Services.Generating.Models.States;

/// <summary>Represents the execution snapshot of a workbook processing activity.</summary>
public class WorkbookSnapshot(
    string activityId,
    IExecutionPayload payload,
    ExecutionContext? executionContext = null,
    IReadOnlyDictionary<string, ExecutionSnapshot>? activities = null,
    IReadOnlyDictionary<string, string>? worksheetActivityMap = null)
    : ActivitySnapshot(activityId, payload, executionContext, activities)
{
    /// <summary>Gets the mapping from worksheet names to activity IDs.</summary>
    public IReadOnlyDictionary<string, string> WorksheetIds { get; } =
        worksheetActivityMap ?? new Dictionary<string, string>();

    /// <summary>Retrieves the snapshot of a specific worksheet activity by its name.</summary>
    public WorksheetSnapshot? GetWorksheet(string worksheetName) =>
        WorksheetIds.TryGetValue(worksheetName, out var id) ? GetActivity<WorksheetSnapshot>(id) : null;
}
