using SlideGenerator.Gate.Models;
using SlideGenerator.Gate.Services;
using SlideGenerator.Services.Generating.Workflows;
using SlideGenerator.Sheets;
using SlideGenerator.Slides.Entities;
using Syncfusion.XlsIO;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Services.Generating.Steps;

public sealed class PrepareIterationTasks(GateLocker gateLocker, ExcelEngine excelEngine) : StepBodyAsync
{
    public ValidatedWorksheet Worksheet { get; set; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingData)context.Workflow.Data;
        int rowCount, shapeCount;

        #region Count Rows

        await gateLocker.AcquireAsync(GateType.ReadWorkbook).ConfigureAwait(false);
        try
        {
            var workbook = excelEngine.Excel.Workbooks.Open(
                Worksheet.Identifier.BookFilePath, ExcelParseOptions.Default,
                true, Worksheet.Identifier.BookPassword);
            var sheet = workbook.Worksheets[Worksheet.Identifier.SheetName];
            rowCount = sheet.CountRows();
            workbook.Close();
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }

        #endregion

        #region Pre-clone template slides

        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            using var wrapper = new SfPresentation(Worksheet.OutputPresentationPath, true); // removed password
            var template = wrapper.Value.Slides[0];
            shapeCount = template.Shapes.Count;

            for (var i = 1; i < rowCount; i++)
                wrapper.Value.Slides.Add(template.Clone());
            wrapper.Save();
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        #endregion

        #region Generate in-memory iteration tasks

        // Hmm, that's so weird, but in the next other activity we'll see them in use.
        Enumerable.Range(1, rowCount).ToList().ForEach(rowIndex => data.RowTasks.Add(new RowTask(Worksheet, rowIndex)));
        Enumerable.Range(0, shapeCount).ToList()
            .ForEach(shapeIndex => data.ShapeTasks.Add(new ShapeTask(Worksheet, shapeIndex)));
        Enumerable.Range(1, rowCount).ToList().ForEach(rowIndex =>
            Enumerable.Range(0, shapeCount).ToList().ForEach(shapeIndex =>
                data.RowShapeTasks.Add(new RowShapeTask(new RowTask(Worksheet, rowIndex),
                    new ShapeTask(Worksheet, shapeIndex)))
            )
        );

        #endregion

        return ExecutionResult.Next();
    }
}