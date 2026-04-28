using SlideGenerator.Application.Modules.Workflows.Entities.Contexts;
using SlideGenerator.Application.Modules.Workflows.Models.States;

namespace SlideGenerator.Application.Services.Generating.Models.States;

/// <summary>
///     Represents the state of a slide generation workflow.
/// </summary>
public class GeneratingState(
    string workflowId,
    IExecutionContext executionContext,
    IReadOnlyDictionary<string, ExecutionState>? activities = null,
    IReadOnlyDictionary<string, string>? workbookActivityMap = null)
    : WorkflowState(workflowId, executionContext, activities)
{
    /// <summary>Gets the mapping from workbook names to activity IDs.</summary>
    public IReadOnlyDictionary<string, string> WorkbookIds { get; } =
        workbookActivityMap ?? new Dictionary<string, string>();

    /// <summary>Retrieves the state of a specific workbook activity by its name.</summary>
    /// <param name="workbookName">The name of the workbook.</param>
    /// <returns>The state if found; otherwise, <see langword="null" />.</returns>
    public WorkbookState? GetWorkbook(string workbookName)
    {
        return WorkbookIds.TryGetValue(workbookName, out var id) ? GetActivity<WorkbookState>(id) : null;
    }
}