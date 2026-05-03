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
        var bookName = Path.GetFileNameWithoutExtension(Task.Worksheet.Identifier.BookPath);

        await gateLocker.AcquireAsync(GateType.ReadWorkbook).ConfigureAwait(false);
        try
        {
            var path = Task.Worksheet.Identifier.BookPath;
            var book = excelEngine.Excel.Workbooks.Open(path, ExcelParseOptions.Default, true,
                Task.Worksheet.Identifier.BookPassword);
            var sheet = book.Worksheets[Task.Worksheet.Identifier.SheetName];

            var headers = sheet.GetHeaders();
            var headerMap = headers.Select((h, i) => (h, i))
                .ToDictionary(x => x.h, x => x.i, StringComparer.OrdinalIgnoreCase);
            var rowData = sheet.GetRow(Task.RowIndex);

            // Generate ImageTasks for this row based on instructions in the associated MapNode
            foreach (var imgInst in Task.Worksheet.MapNode.ImageInstructions)
            {
                foreach (var column in imgInst.Columns)
                {
                    // Only process if the instruction refers to this specific sheet
                    if (!column.SheetName.Equals(sheet.Name, StringComparison.OrdinalIgnoreCase) ||
                        !column.BookPath.Equals(path, StringComparison.OrdinalIgnoreCase)) continue;

                    if (!headerMap.TryGetValue(column.ColumnName, out var colIndex) || colIndex >= rowData.Count) continue;

                    var uri = Utilities.NormalizeUri(rowData[colIndex]);

                    var downloadDir =
                        settingProvider.Current.Download.Temp.GetDownloadDir(bookName, sheet.Name, column.ColumnName);
                    var downloadPath = Path.Combine(downloadDir, Task.RowIndex.ToString());

                    foreach (var shape in imgInst.Shapes)
                    {
                        if (shape.PresentationPath != Task.Worksheet.TemplateSlide.PresentationPath
                            || shape.SlideIndex != Task.Worksheet.TemplateSlide.SlideIndex) continue;
                        if (!data.ShapeBounds.TryGetValue(shape, out var bounds)) continue;

                        var editDir =
                            settingProvider.Current.Download.Temp.GetEditDir(bookName, sheet.Name, column.ColumnName);
                        var editPath = Path.Combine(editDir, $"{Task.RowIndex}_{shape.ShapeName}");

                        data.ImageTasks.Add(
                            new ImageTask(
                                Task.Worksheet.Identifier, Task.RowIndex, column.ColumnName,
                                shape.ShapeName, uri, downloadPath, editPath, (double)bounds.Width, (double)bounds.Height,
                                imgInst.EditOptions, imgInst.FallbackImagePath));
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
