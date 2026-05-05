using Serilog;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Pipelines.Generating.Workflows.Models;
using SlideGenerator.Documents.Slides.Services;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using IShape = Syncfusion.Presentation.IShape;

namespace SlideGenerator.Pipelines.Generating.Steps;

/// <summary>
///     Fills a single slide with pre-calculated text and image replacements.
///     Avoids redundant file I/O by executing all replacements for a slide in one pass.
/// </summary>
public sealed class ReplaceSlideData(GateLocker gateLocker, ImageComposer imageComposer, TextComposer textComposer, ILogger logger) : StepBodyAsync
{
    public SlideTask Task { get; init; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;
        data.TryInitLogger(logger, context.Workflow.Id);

        if (Task.TextReplacements.Count == 0 && Task.ImageReplacements.Count == 0)
        {
            data.Logger.Debug("No replacements found for row {RowIndex} in sheet {SheetName}. Skipping.", Task.RowIndex, Task.SheetTask.Identifier.SheetName);
            return ExecutionResult.Next();
        }

        data.Logger.Information("Starting data replacement for row {RowIndex} in sheet {SheetName}", Task.RowIndex, Task.SheetTask.Identifier.SheetName);

        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            if (data.OutputHandles.TryGetValue(Task.SheetTask.OutputIdentifier, out var wrapper))
            {
                var slide = wrapper.Value.Slides[Task.RowIndex - 1];

                foreach (var shapeBase in slide.Shapes)
                {
                    if (shapeBase is not IShape shape || string.IsNullOrEmpty(shape.ShapeName))
                        continue;

                    data.Logger.Debug("Processing shape '{ShapeName}' for row {RowIndex}", shape.ShapeName, Task.RowIndex);

                    ApplyTextReplacements(data, shape);
                    await ApplyImageReplacementsAsync(data, shape).ConfigureAwait(false);
                }

                wrapper.Save();

                data.Logger.Information("Successfully replaced data for row {RowIndex} in sheet {SheetName}", Task.RowIndex, Task.SheetTask.Identifier.SheetName);
            }
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException and not IndexOutOfRangeException)
        {
            var path = $"{Task.SheetTask.Identifier.SheetName}_{Task.RowIndex}";
            data.Logger.ForContext("Path", path).Error(ex, "FillSlideData failed");
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        return ExecutionResult.Next();
    }

    private void ApplyTextReplacements(GeneratingTask data, IShape shape)
    {
        if (Task.TextReplacements.Count > 0)
        {
            data.Logger.Debug("Applying text replacements to shape '{ShapeName}' (Count: {Count})", shape.ShapeName, Task.TextReplacements.Count);
            textComposer.Replace(shape, Task.TextReplacements);
        }
    }

    private async Task ApplyImageReplacementsAsync(GeneratingTask data, IShape shape)
    {
        var matchingImageTask = Task.ImageReplacements.Values.FirstOrDefault(t =>
            t.ShapeName.Equals(shape.ShapeName, StringComparison.OrdinalIgnoreCase));

        if (matchingImageTask != null)
        {
            var finalEditPath = matchingImageTask.EditPath + ".png";
            if (File.Exists(finalEditPath))
            {
                data.Logger.Information("Replacing image for shape '{ShapeName}' with '{Path}'", shape.ShapeName, finalEditPath);

                await using var imgStream = new FileStream(finalEditPath, FileMode.Open, FileAccess.Read);
                imageComposer.Replace(shape, imgStream);
            }
            else
            {
                data.Logger.Warning("Edited image not found at '{Path}' for shape '{ShapeName}'", finalEditPath, shape.ShapeName);
            }
        }
    }
}
