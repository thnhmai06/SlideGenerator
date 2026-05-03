using SlideGenerator.Gate.Models;
using SlideGenerator.Gate.Services;
using SlideGenerator.Services.Generating.Workflows;
using SlideGenerator.Sheets;
using SlideGenerator.Slides.Entities;
using SlideGenerator.Slides.Services;
using Syncfusion.XlsIO;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using IShape = Syncfusion.Presentation.IShape;

namespace SlideGenerator.Services.Generating.Steps;

public sealed class ReplaceShapeData(GateLocker gateLocker, ExcelEngine excelEngine) : StepBodyAsync
{
    public RowShapeTask Task { get; set; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingData)context.Workflow.Data;
        var worksheet = Task.RowTask.Worksheet;
        var rowIndex = Task.RowTask.RowIndex;
        var shapeIndex = Task.ShapeTask.ShapeIndex;

        var textInstructions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? imagePath = null;
        string? currentShapeName = null;
        HashSet<string> shapeTags = [];

        // 1. Find Shape Name and Mustache Tags
        await gateLocker.AcquireAsync(GateType.ReadPresentation).ConfigureAwait(false);
        try
        {
            using var wrapper = new SfPresentation(worksheet.OutputPresentationPath, false);
            if (wrapper.Value.Slides[rowIndex - 1].Shapes[shapeIndex] is IShape shape)
            {
                currentShapeName = shape.ShapeName;
                shapeTags = TextComposer.Scan(shape).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
        }
        finally
        {
            gateLocker.Release(GateType.ReadPresentation);
        }

        if (string.IsNullOrEmpty(currentShapeName)) return ExecutionResult.Next();

        // 2. Identify relevant instructions from the Worksheet's MapNode
        var relevantTextInstructions = worksheet.MapNode.TextInstructions
            .Where(ti => ti.Placeholders.Any(p => shapeTags.Contains(p)))
            .ToList();

        var relevantImageInstruction = worksheet.MapNode.ImageInstructions
            .FirstOrDefault(img => img.Shapes.Any(s =>
                s.ShapeName.Equals(currentShapeName, StringComparison.OrdinalIgnoreCase) && 
                s.PresentationPath == worksheet.TemplateSlide.PresentationPath));

        bool needsText = relevantTextInstructions.Any();
        bool needsImage = relevantImageInstruction != null;

        if (!needsText && !needsImage) return ExecutionResult.Next();

        // 3. Extract Data from Workbook for this specific row
        await gateLocker.AcquireAsync(GateType.ReadWorkbook).ConfigureAwait(false);
        try
        {
            var workbook = excelEngine.Excel.Workbooks.Open(worksheet.Identifier.BookPath,
                ExcelParseOptions.Default, true, worksheet.Identifier.BookPassword);
            var sheet = workbook.Worksheets[worksheet.Identifier.SheetName];

            var headers = sheet.GetHeaders();
            var headerMap = headers.Select((h, i) => (h, i))
                .ToDictionary(x => x.h, x => x.i, StringComparer.OrdinalIgnoreCase);
            var rowData = sheet.GetRow(rowIndex);

            // Extract text data based on RELEVANT instructions
            if (needsText)
            {
                foreach (var txtInst in relevantTextInstructions)
                {
                    foreach (var column in txtInst.Columns)
                    {
                        // Check if the column belongs to this sheet
                        if (!column.SheetName.Equals(worksheet.Identifier.SheetName,
                                StringComparison.OrdinalIgnoreCase) ||
                            !column.BookPath.Equals(worksheet.Identifier.BookPath,
                                StringComparison.OrdinalIgnoreCase)) continue;
                        if (!headerMap.TryGetValue(column.ColumnName, out var colIndex) 
                            || colIndex >= rowData.Count) continue;
                        
                        var val = rowData[colIndex];
                        foreach (var placeholder in txtInst.Placeholders)
                        {
                            if (shapeTags.Contains(placeholder))
                                textInstructions[placeholder] = val;
                        }
                    }
                }
                workbook.Close();
            }
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }
        
        // Find Image Edit Path
        if (needsImage)
        {
            var matchingTask =
                data.ImageTasks.FirstOrDefault(t => t.RowIndex == rowIndex && 
                                                    t.ShapeName.Equals(currentShapeName, StringComparison.OrdinalIgnoreCase));
            if (matchingTask != null)
            {
                var testPath = matchingTask.EditPath + ".png";
                if (File.Exists(testPath)) imagePath = testPath;
            }
        }

        // 4. Apply Replacement
        if (textInstructions.Count == 0 && imagePath == null) return ExecutionResult.Next();

        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            using var wrapper = new SfPresentation(worksheet.OutputPresentationPath, true);
            var slide = wrapper.Value.Slides[rowIndex - 1];

            if (slide.Shapes[shapeIndex] is IShape shape)
            {
                if (textInstructions.Count > 0)
                    TextComposer.Replace(shape, textInstructions);

                if (imagePath != null)
                {
                    await using var imgStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                    ImageComposer.Replace(shape, imgStream);
                }
            }

            wrapper.Save();
        }
        catch (Exception ex)
        {
            data.Errors.TryAdd($"ReplaceShape_{worksheet.Identifier.SheetName}_{rowIndex}_{shapeIndex}", ex);
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        return ExecutionResult.Next();
    }
}