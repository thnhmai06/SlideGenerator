using Serilog;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Document.Sheet.Models;
using SlideGenerator.Document.Slide.Models;
using SlideGenerator.Pipeline.Generating.Models;
using SlideGenerator.Pipeline.Generating.Workflows.Models;
using Syncfusion.XlsIO;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Pipeline.Generating.Steps;

public sealed record ValidationItem(SheetIdentifier Sheet, MapNode Node);

/// <summary>
///     Validates a single sheet and slide mapping, ensuring both exist and are accessible.
/// </summary>
public sealed class ValidateRequest(ExcelEngine excelEngine, GateLocker gateLocker, ILogger logger) : StepBodyAsync
{
    /// <summary>
    ///     The sheet and its associated map node to validate.
    /// </summary>
    public ValidationItem Item { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;
        data.TryInitLogger(logger, context.Workflow.Id);

        var sheet = Item.Sheet;
        var node = Item.Node;
        var slide = node.Slide;

        data.Logger.Information("Validating request for sheet {SheetName} and slide index {SlideIndex}",
            sheet.SheetName, slide.SlideIndex);

        try
        {
            await ValidateWorksheetAsync(data, sheet).ConfigureAwait(false);
            await ValidatePresentationAndMapOutputAsync(data, sheet, node, slide).ConfigureAwait(false);

            data.Logger.Information("Validation successful for sheet {SheetName}", sheet.SheetName);
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            var path = $"{sheet.BookPath}_{sheet.SheetName}";
            data.Logger.ForContext("Path", path).Error(ex, "Validation failed");
        }

        return ExecutionResult.Next();
    }

    private async Task ValidateWorksheetAsync(GeneratingTask data, SheetIdentifier sheet)
    {
        await gateLocker.AcquireAsync(GateType.ReadWorkbook).ConfigureAwait(false);
        try
        {
            var workbook = data.GetOrOpenWorkbook(excelEngine, sheet);

            var worksheet = workbook.Value.Worksheets[sheet.SheetName];
            if (worksheet == null)
                throw new ArgumentException(
                    $"Sheet '{sheet.SheetName}' not found in workbook '{Path.GetFileName(sheet.BookPath)}'.");

            data.Logger.Debug("Verified workbook '{BookName}' contains sheet '{SheetName}'",
                Path.GetFileName(sheet.BookPath), sheet.SheetName);
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }
    }

    private async Task ValidatePresentationAndMapOutputAsync(GeneratingTask data, SheetIdentifier sheet, MapNode node,
        SlideIdentifier slide)
    {
        await gateLocker.AcquireAsync(GateType.ReadPresentation).ConfigureAwait(false);
        try
        {
            var template = data.GetOrOpenPresentation(slide);

            var presentation = template.Value;
            if (slide.SlideIndex <= 0 || slide.SlideIndex > presentation.Slides.Count)
                throw new ArgumentException(
                    $"Slide index {slide.SlideIndex} is out of range for '{Path.GetFileName(slide.PresentationPath)}' (Count: {presentation.Slides.Count}).");

            data.Logger.Debug("Verified presentation '{PresentationName}' contains slide index {Index}",
                Path.GetFileName(slide.PresentationPath), slide.SlideIndex);

            // Successful validation: Prepare output mapping
            var bookName = Path.GetFileNameWithoutExtension(sheet.BookPath);
            var outputFileName =
                $"{Settings.Utilities.NormalizeFileName(sheet.SheetName)}{Path.GetExtension(slide.PresentationPath)}";
            var outputPath = Path.Combine(data.Request.SaveFolder, bookName, outputFileName);
            var outputIdentifier = new PresentationIdentifier(outputPath);

            data.ValidWorksheets.TryAdd(sheet, new SheetTask(sheet, slide, node, outputIdentifier));

            data.Logger.Debug("Output path mapped to: '{Path}'", outputPath);
        }
        finally
        {
            gateLocker.Release(GateType.ReadPresentation);
        }
    }
}