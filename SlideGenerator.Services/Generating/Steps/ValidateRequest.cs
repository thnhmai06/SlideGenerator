using Serilog;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Services.Generating.Models;
using SlideGenerator.Services.Generating.Models.Identifiers;
using SlideGenerator.Services.Generating.Workflows.Models;
using Syncfusion.XlsIO;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Services.Generating.Steps;

public sealed record ValidationItem(SheetIdentifier Sheet, MapNode Node);

/// <summary>
///     Validates a single sheet and slide mapping, ensuring both exist and are accessible.
/// </summary>
public sealed class ValidateRequest(ExcelEngine excelEngine, GateLocker gateLocker, ILogger logger) : StepBodyAsync
{
    /// <summary>
    ///     The sheet and its associated map node to validate.
    /// </summary>
    public ValidationItem Item { get; set; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;
        var sheet = Item.Sheet;
        var node = Item.Node;
        var slide = node.Slide;

        logger.ForContext("TaskId", context.Workflow.Id)
            .Information("Validating request for sheet {SheetName} and slide index {SlideIndex}", sheet.SheetName, slide.SlideIndex);

        try
        {
            await ValidateWorksheetAsync(context, data, sheet).ConfigureAwait(false);
            await ValidatePresentationAndMapOutputAsync(context, data, sheet, node, slide).ConfigureAwait(false);
            
            logger.ForContext("TaskId", context.Workflow.Id)
                .Information("Validation successful for sheet {SheetName}", sheet.SheetName);
        }
        catch (Exception ex)
        {
            var path = $"{sheet.BookPath}_{sheet.SheetName}";
            logger.ForContext("TaskId", context.Workflow.Id).ForContext("Path", path).Error(ex, "Validation failed");
        }

        return ExecutionResult.Next();
    }

    private async Task ValidateWorksheetAsync(IStepExecutionContext context, GeneratingTask data, SheetIdentifier sheet)
    {
        await gateLocker.AcquireAsync(GateType.ReadWorkbook).ConfigureAwait(false);
        try
        {
            var workbook = data.GetOrOpenWorkbook(excelEngine, sheet);

            var worksheet = workbook.Worksheets[sheet.SheetName];
            if (worksheet == null)
                throw new ArgumentException(
                    $"Sheet '{sheet.SheetName}' not found in workbook '{Path.GetFileName(sheet.BookPath)}'.");
            
            logger.ForContext("TaskId", context.Workflow.Id)
                .Debug("Verified workbook '{BookName}' contains sheet '{SheetName}'", Path.GetFileName(sheet.BookPath), sheet.SheetName);
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }
    }

    private async Task ValidatePresentationAndMapOutputAsync(IStepExecutionContext context, GeneratingTask data, SheetIdentifier sheet, MapNode node,
        SlideIdentifier slide)
    {
        await gateLocker.AcquireAsync(GateType.ReadPresentation).ConfigureAwait(false);
        try
        {
            var template = data.GetOrOpenTemplate(slide);

            var presentation = template.Value;
            if (slide.SlideIndex <= 0 || slide.SlideIndex > presentation.Slides.Count)
                throw new ArgumentException(
                    $"Slide index {slide.SlideIndex} is out of range for '{Path.GetFileName(slide.PresentationPath)}' (Count: {presentation.Slides.Count}).");

            logger.ForContext("TaskId", context.Workflow.Id)
                .Debug("Verified presentation '{PresentationName}' contains slide index {Index}", Path.GetFileName(slide.PresentationPath), slide.SlideIndex);

            // Successful validation: Prepare output mapping
            var bookName = Path.GetFileNameWithoutExtension(sheet.BookPath);
            var outputFileName =
                $"{Settings.Utilities.NormalizeFileName(sheet.SheetName)}{Path.GetExtension(slide.PresentationPath)}";
            var outputPath = Path.Combine(data.Request.SaveFolder, bookName, outputFileName);

            data.ValidWorksheets.TryAdd(sheet, new SheetTask(sheet, slide, node, outputPath));
            
            logger.ForContext("TaskId", context.Workflow.Id)
                .Debug("Output path mapped to: '{Path}'", outputPath);
        }
        finally
        {
            gateLocker.Release(GateType.ReadPresentation);
        }
    }
}