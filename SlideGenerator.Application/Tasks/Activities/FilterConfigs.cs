using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Tasks.Models.Image;

namespace SlideGenerator.Application.Tasks.Activities;

using Worksheets = IReadOnlyDictionary<string, IReadOnlyWorksheet>;

public class FilterConfigs : Activity
{
    public Input<IReadOnlyList<Domain.Tasks.Models.Text.GeneralInstruction>> RawTextConfigs { get; set; } = null!;

    public Input<IReadOnlyList<GeneralInstruction>> RawImageConfigs { get; set; } = null!;

    public Input<string> SheetName { get; set; } = null!;

    public Output<IReadOnlyList<Domain.Tasks.Models.Text.GeneralInstruction>> TextConfigs { get; set; } = null!;

    public Output<IReadOnlyList<GeneralInstruction>> ImageConfigs { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var sheetName = context.Get(SheetName);
        var rawTextConfigs = context.Get(RawTextConfigs);
        var rawImageConfigs = context.Get(RawImageConfigs);
        var worksheets = context.WorkflowExecutionContext.GetProperty<Worksheets>("Worksheets");

        if (rawTextConfigs is null || rawImageConfigs is null || worksheets is null || sheetName is null ||
            !worksheets.TryGetValue(sheetName, out var worksheet))
            return ValueTask.CompletedTask;

        var columnNames = worksheet.GetHeadersName().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (columnNames.Count == 0)
            return ValueTask.CompletedTask;

        var textConfigs = rawTextConfigs
            .Where(config => config.Sources.Any(column => columnNames.Contains(column.ColumnName)))
            .ToList();
        var imageConfigs = rawImageConfigs
            .Where(config => config.Sources.Any(column => columnNames.Contains(column.ColumnName)))
            .ToList();

        context.Set(TextConfigs, textConfigs);
        context.Set(ImageConfigs, imageConfigs);
        return ValueTask.CompletedTask;
    }
}