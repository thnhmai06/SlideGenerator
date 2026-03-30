using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Tasks.Models;
using SlideGenerator.Domain.Tasks.Models;
using SlideGenerator.Domain.Sheet.Models;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Reads <see cref="GenerateRequest"/> and extracts deterministic worksheet-slide work items.
/// </summary>
/// <remarks>
///     <para>State usage:</para>
///     <list type="bullet">
///         <item><description>Writes lightweight outputs only: <see cref="WorkItems"/> and <see cref="Workbooks"/>.</description></item>
///         <item><description>Does not store streams, workbook contents, or other temporary runtime resources.</description></item>
///     </list>
///     <para>
///         The activity is idempotent: rerunning with the same input request produces identical outputs.
///     </para>
/// </remarks>
public sealed class ScanGraphRequirements : Activity
{
    /// <summary>
    ///     Input generation request that contains the worksheet-slide graph.
    /// </summary>
    public Input<GenerateRequest> Request { get; set; } = null!;

    /// <summary>
    ///     Output deterministic worksheet-slide pairs to process.
    /// </summary>
    public Output<IReadOnlyList<WorkingItem>> WorkItems { get; set; } = null!;

    /// <summary>
    ///     Output distinct workbooks needed by the graph.
    /// </summary>
    public Output<IReadOnlyList<WorkbookIdentifier>> Workbooks { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = context.Get(Request);
        if (request?.Graph is null || request.Graph.Count == 0)
        {
            context.Set(WorkItems, []);
            context.Set(Workbooks, []);
            return ValueTask.CompletedTask;
        }

        var ordered = request.Graph
            .OrderBy(kvp => kvp.Key.Workbook.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(kvp => kvp.Key.Name, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => new WorkingItem(kvp.Key, kvp.Value))
            .ToList();

        var workbooks = ordered
            .Select(item => item.Worksheet.Workbook)
            .Distinct()
            .ToList();

        context.Set(WorkItems, ordered);
        context.Set(Workbooks, workbooks);
        return ValueTask.CompletedTask;
    }
}