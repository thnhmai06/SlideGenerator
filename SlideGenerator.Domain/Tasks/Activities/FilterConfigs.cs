using ClosedXML.Excel;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Tasks.Models;
using SlideGenerator.Framework.Sheet.Services;

namespace SlideGenerator.Domain.Tasks.Activities;

using Worksheets = IReadOnlyDictionary<string, IXLWorksheet>;

public class FilterConfigs : WorkflowBase
{
    public Input<IReadOnlyList<TextConfig>> RawTextConfigs { get; set; } = null!;

    public Input<IReadOnlyList<ImageConfig>> RawImageConfigs { get; set; } = null!;

    public Input<string> SheetName { get; set; } = null!;

    public Output<IReadOnlyList<TextConfig>> TextConfigs { get; set; } = null!;

    public Output<IReadOnlyList<ImageConfig>> ImageConfigs { get; set; } = null!;

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Inline(context =>
        {
            var sheetName = context.Get(SheetName);
            var rawTextConfigs = context.Get(RawTextConfigs);
            var rawImageConfigs = context.Get(RawImageConfigs);
            var worksheets = context.WorkflowExecutionContext.GetProperty<Worksheets>("Worksheets");

            if (rawTextConfigs is null || rawImageConfigs is null || worksheets is null || sheetName is null ||
                !worksheets.TryGetValue(sheetName, out var worksheet)) return;

            var contentRange = worksheet.GetContentRange();
            var columnNames = contentRange?.GetHeadersName().ToHashSet();
            if (columnNames is null) return;

            var textConfigs = rawTextConfigs
                .Where(config => config.Columns.Any(column => columnNames.Contains(column)))
                .ToList();
            var imageConfigs = rawImageConfigs
                .Where(config => config.Columns.Any(column => columnNames.Contains(column)))
                .ToList();

            context.Set(TextConfigs, textConfigs);
            context.Set(ImageConfigs, imageConfigs);
        })
        {
            Name = "FilterConfigs"
        };
    }
}