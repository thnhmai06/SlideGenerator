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
public sealed class ValidateRequest(ExcelEngine excelEngine, GateLocker gateLocker) : StepBodyAsync
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

        try
        {
            await ValidateWorksheetAsync(data, sheet).ConfigureAwait(false);
            await ValidatePresentationAndMapOutputAsync(data, sheet, node, slide).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            data.Errors.TryAdd($"Validation_{sheet.BookPath}_{sheet.SheetName}", ex);
        }

        return ExecutionResult.Next();
    }

    private async Task ValidateWorksheetAsync(GeneratingTask data, SheetIdentifier sheet)
    {
        await gateLocker.AcquireAsync(GateType.ReadWorkbook).ConfigureAwait(false);
        try
        {
            var workbook = data.GetOrOpenWorkbook(excelEngine, sheet);

            var worksheet = workbook.Worksheets[sheet.SheetName];
            if (worksheet == null)
                throw new ArgumentException(
                    $"Sheet '{sheet.SheetName}' not found in workbook '{Path.GetFileName(sheet.BookPath)}'.");
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
            var template = data.GetOrOpenTemplate(slide);

            var presentation = template.Value;
            if (slide.SlideIndex <= 0 || slide.SlideIndex > presentation.Slides.Count)
                throw new ArgumentException(
                    $"Slide index {slide.SlideIndex} is out of range for '{Path.GetFileName(slide.PresentationPath)}' (Count: {presentation.Slides.Count}).");

            // Successful validation: Prepare output mapping
            var bookName = Path.GetFileNameWithoutExtension(sheet.BookPath);
            var outputFileName =
                $"{Settings.Utilities.NormalizeFileName(sheet.SheetName)}{Path.GetExtension(slide.PresentationPath)}";
            var outputPath = Path.Combine(data.Request.SaveFolder, bookName, outputFileName);

            data.ValidWorksheets.TryAdd(sheet, new SheetTask(sheet, slide, node, outputPath));
        }
        finally
        {
            gateLocker.Release(GateType.ReadPresentation);
        }
    }
}