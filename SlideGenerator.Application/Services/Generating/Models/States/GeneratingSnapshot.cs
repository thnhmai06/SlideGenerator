using ExecutionContext = SlideGenerator.Application.Modules.Workflows.Models.States.IExecutionContext;
using SlideGenerator.Application.Modules.Workflows.Models.States;

namespace SlideGenerator.Application.Services.Generating.Models.States;

/// <summary>Represents the execution snapshot of a slide generation workflow.</summary>
public class GeneratingSnapshot(
    string workflowId,
    IExecutionPayload payload,
    ExecutionContext? executionContext = null,
    IReadOnlyDictionary<string, ExecutionSnapshot>? activities = null,
    IReadOnlyDictionary<string, string>? workbookActivityMap = null)
    : WorkflowSnapshot(workflowId, payload, executionContext, activities)
{
    /// <summary>Gets the mapping from workbook names to activity IDs.</summary>
    public IReadOnlyDictionary<string, string> WorkbookIds { get; } =
        workbookActivityMap ?? new Dictionary<string, string>();

    /// <summary>Retrieves the snapshot of a specific workbook activity by its name.</summary>
    public WorkbookSnapshot? GetWorkbook(string workbookName) =>
        WorkbookIds.TryGetValue(workbookName, out var id) ? GetActivity<WorkbookSnapshot>(id) : null;
}
