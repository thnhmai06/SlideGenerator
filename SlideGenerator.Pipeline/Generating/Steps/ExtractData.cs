using System.Drawing;
using Serilog;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Document.Sheet;
using SlideGenerator.Document.Slide;
using SlideGenerator.Document.Slide.Models;
using SlideGenerator.Document.Slide.Services;
using SlideGenerator.Pipeline.Generating.Workflows.Models;
using SlideGenerator.Settings.Services;
using Syncfusion.XlsIO;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using IShape = Syncfusion.Presentation.IShape;

namespace SlideGenerator.Pipeline.Generating.Steps;

/// <summary>
///     Consolidates data extraction into a single phase per worksheet.
///     Opens Excel and Presentation once, clones template slides,
///     and generates all SlideTasks and ImageTasks required for the worksheet.
/// </summary>
public sealed class ExtractData(
    GateLocker gateLocker,
    ExcelEngine excelEngine,
    ISettingProvider settingProvider,
    TextComposer textComposer,
    ILogger logger)
    : StepBodyAsync
{
    public SheetTask Worksheet { get; init; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;
        data.TryInitLogger(logger, context.Workflow.Id);

        data.Logger.Information("Starting data extraction for sheet {SheetName} in {BookPath}",
            Worksheet.Identifier.SheetName, Worksheet.Identifier.BookPath);

        try
        {
            var (rowCount, headerMap, sheet) = await ReadWorkbookMetadataAsync(data).ConfigureAwait(false);
            var shapeData = await CloneSlidesAndExtractShapeDataAsync(data, rowCount).ConfigureAwait(false);

            ConstructTasks(data, rowCount, headerMap, sheet, shapeData);

            data.Logger.Information("Successfully extracted data and constructed tasks for sheet {SheetName}",
                Worksheet.Identifier.SheetName);
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            data.Logger.ForContext("Path", Worksheet.Identifier.SheetName).Error(ex, "ExtractData failed");
        }

        return ExecutionResult.Next();
    }

    private async Task<(int RowCount, Dictionary<string, int> HeaderMap, IWorksheet Sheet)>
        ReadWorkbookMetadataAsync(GeneratingTask data)
    {
        await gateLocker.AcquireAsync(GateType.ReadWorkbook).ConfigureAwait(false);
        try
        {
            var workbook = data.GetOrOpenWorkbook(excelEngine, Worksheet.Identifier);

            var sheet = workbook.Value.Worksheets[Worksheet.Identifier.SheetName];
            var rowCount = sheet.CountRows();

            var headers = sheet.GetHeaders();
            var headerMap = headers.Select((h, i) => (h, i))
                .ToDictionary(x => x.h, x => x.i, StringComparer.OrdinalIgnoreCase);

            data.Logger.Debug("Found {RowCount} rows in sheet {SheetName}", rowCount, Worksheet.Identifier.SheetName);

            return (rowCount, headerMap, sheet);
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }
    }

    private async Task<Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)>>
        CloneSlidesAndExtractShapeDataAsync(GeneratingTask data, int rowCount)
    {
        var shapeData = new Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)>();

        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            if (data.OutputHandles.TryGetValue(Worksheet.OutputIdentifier, out var wrapper))
            {
                var templateSlide = wrapper.Value.Slides[0];

                // Extract template shape info
                foreach (var slideItem in templateSlide.Shapes)
                {
                    if (slideItem is not IShape shape || string.IsNullOrEmpty(shape.ShapeName)) continue;

                    var shapeId = new ShapeIdentifier(
                        Worksheet.TemplateSlide.PresentationPath,
                        Worksheet.TemplateSlide.SlideIndex,
                        shape.ShapeName,
                        Worksheet.TemplateSlide.PresentationPassword);

                    var tags = textComposer.Scan(shape);
                    var bounds = shape.GetBoundsF();
                    shapeData[shapeId] = (shape.ShapeName, tags, bounds);
                }

                data.Logger.Debug("Extracted metadata for {Count} shapes from template slide", shapeData.Count);

                // Clone slides
                if (rowCount > 1)
                {
                    data.Logger.Debug("Cloning {Count} additional slides for output", rowCount - 1);
                    for (var i = 1; i < rowCount; i++)
                        wrapper.Value.Slides.Add(templateSlide.Clone());
                }

                wrapper.Save();
            }
            else
            {
                throw new KeyNotFoundException(
                    $"Output handle not found for {Worksheet.OutputIdentifier.PresentationPath}");
            }
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        return shapeData;
    }

    private void ConstructTasks(
        GeneratingTask data,
        int rowCount,
        Dictionary<string, int> headerMap,
        IWorksheet sheet,
        Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)> shapeData)
    {
        var bookName = Path.GetFileNameWithoutExtension(Worksheet.Identifier.BookPath);

        for (var rowIndex = 1; rowIndex <= rowCount; rowIndex++)
        {
            var slideTask = new SlideTask(Worksheet, rowIndex);
            var rowData = sheet.GetRow(rowIndex);

            MapTextReplacements(slideTask, rowData, headerMap, sheet, shapeData);
            MapImageReplacements(data, slideTask, rowData, headerMap, sheet, shapeData, bookName, rowIndex);

            if (slideTask.TextReplacements.Count > 0 || slideTask.ImageReplacements.Count > 0)
            {
                data.SlideTasks.Add(slideTask);
                data.Logger.Debug("Mapped {TextCount} text and {ImageCount} image replacements for row {RowIndex}",
                    slideTask.TextReplacements.Count, slideTask.ImageReplacements.Count, rowIndex);
            }
        }
    }

    private void MapTextReplacements(
        SlideTask slideTask,
        IReadOnlyList<string> rowData,
        Dictionary<string, int> headerMap,
        IWorksheet sheet,
        Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)> shapeData)
    {
        foreach (var textInst in Worksheet.MapNode.TextInstructions)
        foreach (var column in textInst.Columns)
        {
            if (!column.SheetName.Equals(sheet.Name, StringComparison.OrdinalIgnoreCase) ||
                !column.BookPath.Equals(Worksheet.Identifier.BookPath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!headerMap.TryGetValue(column.ColumnName, out var colIndex) || colIndex >= rowData.Count) continue;

            var val = rowData[colIndex];

            foreach (var placeholder in textInst.Placeholders)
            {
                var isUsed = shapeData.Values.Any(s => s.Tags.Contains(placeholder));
                if (isUsed) slideTask.TextReplacements[placeholder] = val;
            }
        }
    }

    private void MapImageReplacements(
        GeneratingTask data,
        SlideTask slideTask,
        IReadOnlyList<string> rowData,
        Dictionary<string, int> headerMap,
        IWorksheet sheet,
        Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)> shapeData,
        string bookName,
        int rowIndex)
    {
        foreach (var imgInst in Worksheet.MapNode.ImageInstructions)
        foreach (var column in imgInst.Columns)
        {
            if (!column.SheetName.Equals(sheet.Name, StringComparison.OrdinalIgnoreCase) ||
                !column.BookPath.Equals(Worksheet.Identifier.BookPath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!headerMap.TryGetValue(column.ColumnName, out var colIndex) || colIndex >= rowData.Count) continue;

            var uri = Settings.Utilities.NormalizeUri(rowData[colIndex]);
            var downloadDir =
                settingProvider.Current.Download.Temp.GetDownloadDir(bookName, sheet.Name, column.ColumnName);
            var downloadPath = Path.Combine(downloadDir, rowIndex.ToString());

            foreach (var shapeId in imgInst.Shapes)
            {
                if (shapeId.PresentationPath != Worksheet.TemplateSlide.PresentationPath ||
                    shapeId.SlideIndex != Worksheet.TemplateSlide.SlideIndex) continue;
                if (!shapeData.TryGetValue(shapeId, out var sData)) continue;

                var editDir =
                    settingProvider.Current.Download.Temp.GetEditDir(bookName, sheet.Name, column.ColumnName);
                var editPath = Path.Combine(editDir, $"{rowIndex}_{sData.ShapeName}");

                var imageTask = new ImageTask(
                    Worksheet.Identifier, rowIndex, column.ColumnName,
                    sData.ShapeName, uri, downloadPath, editPath,
                    sData.Bounds.Width, sData.Bounds.Height,
                    imgInst.EditOptions, imgInst.FallbackImagePath);

                data.ImageTasks.Add(imageTask);
                slideTask.ImageReplacements[shapeId] = imageTask;
            }
        }
    }
}