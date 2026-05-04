using Serilog;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Pipelines.Generating.Workflows.Models;
using SlideGenerator.Slides.Entities;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Pipelines.Generating.Steps;

/// <summary>
///     Creates the output presentation file by copying the template and isolating
///     the single template slide to be used for cloning.
/// </summary>
public sealed class CreateTemplate(GateLocker gateLocker, ILogger logger) : StepBodyAsync
{
    /// <summary>
    ///     The validation item containing sheet and node info.
    /// </summary>
    public ValidationItem Item { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;
        data.TryInitLogger(logger, context.Workflow.Id);

        if (!data.ValidWorksheets.TryGetValue(Item.Sheet, out var worksheet))
        {
            var ex = new KeyNotFoundException($"Worksheet '{Item.Sheet.SheetName}' was not found in validated results.");
            data.Logger.ForContext("Path", Item.Sheet.SheetName).Error(ex, "CreateTemplate validation failed");
        }
        else
        {
            try
            {
                data.Logger.Information("Initializing output template for sheet {SheetName}", worksheet.Identifier.SheetName);

                await CreateTemplateFileAsync(data, worksheet).ConfigureAwait(false);

                data.Logger.Information("Successfully initialized output presentation at '{Path}'", worksheet.OutputPath);
            }
            catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException and not IndexOutOfRangeException)
            {
                data.Logger.ForContext("Path", worksheet.Identifier.SheetName).Error(ex, "CreateTemplate execution failed");
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
            data.Logger.Debug("Created output directory: '{Path}'", outputDir);
        }

        // 2. Copy the template to the output path (overwrite if it exists)
        data.Logger.Debug("Copying template from '{Source}' to '{Destination}'",
            validatedSheet.TemplateSlide.PresentationPath, validatedSheet.OutputPath);
        File.Copy(validatedSheet.TemplateSlide.PresentationPath, validatedSheet.OutputPath);

        // 3. Isolate template slide (Delete all other slides)
        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            data.Logger.Debug("Isolating slide at index {Index} in output presentation", validatedSheet.TemplateSlide.SlideIndex);

            var wrapper = new SfPresentation(
                validatedSheet.OutputPath, true,
                validatedSheet.TemplateSlide.PresentationPassword);
            data.OutputHandles.TryAdd(validatedSheet.OutputPath, wrapper);

            var presentation = wrapper.Value;

            // Remove all slides except the one at targetIndex.
            // Iterate backwards to safely remove by index.
            var templateIndex = validatedSheet.TemplateSlide.SlideIndex - 1;
            var originalCount = presentation.Slides.Count;
            for (var i = presentation.Slides.Count - 1; i >= 0; i--)
                if (i != templateIndex) presentation.Slides.RemoveAt(i);

            data.Logger.Debug("Removed {Count} unrelated slides from the template copy", originalCount - 1);

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