using ClosedXML.Excel;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using SlideGenerator.Framework.Sheet.Services;

namespace SlideGenerator.Domain.Tasks.Activities;
using Worksheets = IReadOnlyDictionary<string, IXLWorksheet>;
using RowContent = IReadOnlyDictionary<string, string>;

public class GetRowContent : WorkflowBase
{
    public Input<Worksheets> Worksheets { get; set; } = null!;

    public Input<string> SheetName { get; set; } = null!;

    public Input<int> RowIndex { get; set; } = null!;

    public Output<RowContent> RowContent { get; set; } = null!;

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Inline(context =>
        {
            var worksheets = context.Get(Worksheets);
            var sheetName = context.Get(SheetName);
            var rowIndex = context.Get(RowIndex);

            if (worksheets is null || string.IsNullOrEmpty(sheetName) ||
                !worksheets.TryGetValue(sheetName, out var worksheet))
                return;

            var contents = worksheet.GetContentRange();
            var content = contents?.GetRowContent(rowIndex);
            if (content is null) 
                return;

            context.Set(RowContent, content);
        })
        {
            Name = "GetRowContent"
        };
    }
}