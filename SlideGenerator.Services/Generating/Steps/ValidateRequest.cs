using SlideGenerator.Gate.Models;
using SlideGenerator.Gate.Services;
using SlideGenerator.Services.Generating.Models;
using SlideGenerator.Services.Generating.Models.Identifiers;
using SlideGenerator.Services.Generating.Workflows;
using SlideGenerator.Settings;
using SlideGenerator.Slides.Entities;
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
        var data = (GeneratingData)context.Workflow.Data;
        var sheet = Item.Sheet;
        var node = Item.Node;
        var slide = node.Slide;

        try
        {
            await ValidateSheetAndSlideAsync(data, sheet, node, slide).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            data.Errors.TryAdd($"Validation_{sheet.BookFilePath}_{sheet.SheetName}", ex);
        }

        return ExecutionResult.Next();
    }

    private async Task ValidateSheetAndSlideAsync(GeneratingData data, SheetIdentifier sheet, MapNode node, SlideIdentifier slide)
    {
        #region 1. Validate Worksheet

        await gateLocker.AcquireAsync(GateType.ReadWorkbook).ConfigureAwait(false);
        try
        {
            if (!File.Exists(sheet.BookFilePath))
                throw new FileNotFoundException("Workbook not found.", sheet.BookFilePath);

            var workbook =
                excelEngine.Excel.Workbooks.Open(sheet.BookFilePath, ExcelParseOptions.Default, true,
                    sheet.BookPassword);
            try
            {
                var worksheet = workbook.Worksheets[sheet.SheetName];
                if (worksheet == null)
                    throw new ArgumentException(
                        $"Sheet '{sheet.SheetName}' not found in workbook '{Path.GetFileName(sheet.BookFilePath)}'.");
            }
            finally
            {
                workbook.Close();
            }
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }

        #endregion

        #region 2. Validate Slide & Output

        await gateLocker.AcquireAsync(GateType.ReadPresentation).ConfigureAwait(false);
        try
        {
            if (!File.Exists(slide.PresentationFilePath))
                throw new FileNotFoundException("Presentation template not found.", slide.PresentationFilePath);

            using var wrapper = new SfPresentation(slide.PresentationFilePath, false, slide.PresentationPassword);
            var presentation = wrapper.Value;

            if (slide.SlideIndex == 0 || slide.SlideIndex > presentation.Slides.Count)
                throw new ArgumentException(
                    $"Slide index {slide.SlideIndex} is out of range for '{Path.GetFileName(slide.PresentationFilePath)}' (Count: {presentation.Slides.Count}).");

            // Successful validation: Prepare output mapping
            var bookName = Path.GetFileNameWithoutExtension(sheet.BookFilePath);
            var outputFileName =
                $"{Utilities.NormalizeFileName(sheet.SheetName)}{Path.GetExtension(slide.PresentationFilePath)}";
            var outputPath = Path.Combine(data.Request.SaveFolder, bookName, outputFileName);

            data.ValidWorksheets.TryAdd(sheet, new ValidatedWorksheet(sheet, outputPath, slide, node));
        }
        finally
        {
            gateLocker.Release(GateType.ReadPresentation);
        }

        #endregion
    }
}