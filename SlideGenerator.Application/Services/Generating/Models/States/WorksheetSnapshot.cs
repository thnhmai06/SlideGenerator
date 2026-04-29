using SlideGenerator.Application.Modules.Workflows.Models.States;

namespace SlideGenerator.Application.Services.Generating.Models.States;

/// <summary>Represents the execution snapshot of a worksheet processing activity.</summary>
public class WorksheetSnapshot(
    string activityId,
    IExecutionPayload payload,
    WorksheetContext context,
    IReadOnlyDictionary<string, ExecutionSnapshot>? activities = null,
    IReadOnlyDictionary<int, string>? rowActivityMap = null)
    : ActivitySnapshot(activityId, payload, context, activities)
{
    /// <summary>Gets the worksheet-scoped transient execution context.</summary>
    public new WorksheetContext Context =>
        (WorksheetContext)base.Context!;

    /// <summary>Gets the mapping from row indices to activity IDs.</summary>
    public IReadOnlyDictionary<int, string> RowIds { get; } =
        rowActivityMap ?? new Dictionary<int, string>();

    /// <summary>Retrieves the snapshot of a specific row activity by its index.</summary>
    public ActivitySnapshot? GetRow(int rowIndex)
    {
        return RowIds.TryGetValue(rowIndex, out var id) ? GetActivity<ActivitySnapshot>(id) : null;
    }
}