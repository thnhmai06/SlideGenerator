using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Services.Generating.Workflows.Models;
using SlideGenerator.Slides.Entities;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Services.Generating.Steps;

/// <summary>
///     Creates the output presentation file by copying the template and isolating
///     the single template slide to be used for cloning.
/// </summary>
public sealed class CreateTemplate(GateLocker gateLocker) : StepBodyAsync
{
    /// <summary>
    ///     The validation item containing sheet and node info.
    /// </summary>
    public ValidationItem Item { get; set; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;

        if (!data.ValidWorksheets.TryGetValue(Item.Sheet, out var worksheet))
        {
            var ex = new KeyNotFoundException($"Worksheet '{Item.Sheet.SheetName}' was not found in validated results.");
            data.Errors.TryAdd($"CreateTemplate_{Item.Sheet.SheetName}", ex);
        }
        else
        {
            try
            {
                await CreateTemplateFileAsync(data, worksheet).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                data.Errors.TryAdd($"CreateTemplate_{worksheet.Identifier.SheetName}", ex);
            }
        }

        return ExecutionResult.Next();
    }

    private async Task CreateTemplateFileAsync(GeneratingTask data, SheetTask validatedSheet)
    {
        // 1. Ensure output directory exists
        var outputDir = Path.GetDirectoryName(validatedSheet.OutputPath);
        if (outputDir != null)
        {
            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            Directory.CreateDirectory(outputDir);
        }

        // 2. Copy the template to the output path (overwrite if it exists)
        File.Copy(validatedSheet.TemplateSlide.PresentationPath, validatedSheet.OutputPath);

        // 3. Isolate template slide (Delete all other slides)
        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            var wrapper = new SfPresentation(
                validatedSheet.OutputPath, true,
                validatedSheet.TemplateSlide.PresentationPassword);
            data.OutputHandles.TryAdd(validatedSheet.OutputPath, wrapper);

            var presentation = wrapper.Value;
            
            // Remove all slides except the one at targetIndex.
            // Iterate backwards to safely remove by index.
            var templateIndex = validatedSheet.TemplateSlide.SlideIndex - 1;
            for (var i = presentation.Slides.Count - 1; i >= 0; i--)
                if (i != templateIndex) presentation.Slides.RemoveAt(i);
            
            presentation.RemoveEncryption();
            presentation.RemoveWriteProtection();
            wrapper.Save();
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }
    }
}