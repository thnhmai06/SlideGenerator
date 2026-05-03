using SlideGenerator.Gate.Models;
using SlideGenerator.Gate.Services;
using SlideGenerator.Services.Generating.Workflows;
using SlideGenerator.Settings.Interfaces;
using SlideGenerator.Sheets;
using Syncfusion.XlsIO;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Utilities = SlideGenerator.Settings.Utilities;

namespace SlideGenerator.Services.Generating.Steps;

public sealed class ExtractRowData(GateLocker gateLocker, ExcelEngine excelEngine, ISettingProvider settingProvider)
    : StepBodyAsync
{
    public RowTask Task { get; set; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingData)context.Workflow.Data;
        var bookName = Path.GetFileNameWithoutExtension(Task.Worksheet.Identifier.BookFilePath);

        await gateLocker.AcquireAsync(GateType.ReadWorkbook).ConfigureAwait(false);
        try
        {
            var path = Task.Worksheet.Identifier.BookFilePath;
            var book = excelEngine.Excel.Workbooks.Open(path, ExcelParseOptions.Default, true,
                Task.Worksheet.Identifier.BookPassword);
            var sheet = book.Worksheets[Task.Worksheet.Identifier.SheetName];

            var headers = sheet.GetHeaders();
            var headerMap = headers.Select((h, i) => (h, i))
                .ToDictionary(x => x.h, x => x.i, StringComparer.OrdinalIgnoreCase);
            var rowData = sheet.GetRow(Task.RowIndex);

            // Generate ImageTasks for this row based on in-memory instructions
            foreach (var imgInst in data.Request.Recipe.ImageInstructions)
            {
                foreach (var colName in imgInst.ColumnNames)
                {
                    if (!headerMap.TryGetValue(colName, out var colIndex) || colIndex >= rowData.Count) continue;

                    var uri = Utilities.NormalizeUri(rowData[colIndex]);

                    var downloadDir =
                        settingProvider.Current.Download.Temp.GetDownloadDir(bookName, sheet.Name, colName);
                    var downloadPath = Path.Combine(downloadDir, Task.RowIndex.ToString());

                    foreach (var shapeId in imgInst.Shapes)
                    {
                        if (shapeId.PresentationFilePath != Task.Worksheet.TemplateSlide.PresentationFilePath
                            || shapeId.SlideIndex != Task.Worksheet.TemplateSlide.SlideIndex) continue;
                        if (!data.ShapeBounds.TryGetValue(shapeId.ShapeId, out var bounds)) continue;

                        var editDir =
                            settingProvider.Current.Download.Temp.GetEditDir(bookName, sheet.Name, colName);
                        var editPath = Path.Combine(editDir, $"{Task.RowIndex}_{shapeId.ShapeId}");

                        data.ImageTasks.Add(
                            new ImageTask(
                                Task.Worksheet.Identifier, Task.RowIndex, colName,
                                shapeId.ShapeId, uri, downloadPath, editPath, bounds.Width, bounds.Height,
                                imgInst.EditOptions));
                    }
                }
            }

            book.Close();
        }
        catch (Exception ex)
        {
            data.Errors.TryAdd($"ExtractRow_{Task.Worksheet.Identifier.SheetName}_{Task.RowIndex}", ex);
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }

        return ExecutionResult.Next();
    }
}
