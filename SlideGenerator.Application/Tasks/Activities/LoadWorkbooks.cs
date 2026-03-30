using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Loads a collection of Excel workbooks into workbook registry for downstream workflow activities.
/// </summary>
/// <remarks>
///     This activity iterates input items with <see cref="ParallelForEach{T}"/> and opens each file through
///     <see cref="IRegistry{T}"/>.
/// </remarks>
public sealed class LoadWorkbooks : Activity
{
    /// <summary>
    ///     Gets or sets workbook descriptors that should be opened for the current workflow run.
    /// </summary>
    /// <remarks>
    ///     Empty, null, or non-existing paths are skipped silently by design.
    /// </remarks>
    public Input<ICollection<WorkbookIdentifier>> Workbooks { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the workbook registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IReadOnlyWorkbook> WorkbookRegistry { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workbooks = context.Get(Workbooks) ?? [];

        foreach (var workbook in workbooks)
        {
            if (workbook is null || string.IsNullOrEmpty(workbook.FilePath))
                continue;
            if (!File.Exists(workbook.FilePath))
                continue;

            WorkbookRegistry.GetOrOpen(workbook.FilePath);
        }

        return ValueTask.CompletedTask;
    }
}