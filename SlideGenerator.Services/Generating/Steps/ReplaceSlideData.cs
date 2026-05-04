using Serilog;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Services.Generating.Workflows.Models;
using SlideGenerator.Slides.Services;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using IShape = Syncfusion.Presentation.IShape;

namespace SlideGenerator.Services.Generating.Steps;

/// <summary>
///     Fills a single slide with pre-calculated text and image replacements.
///     Avoids redundant file I/O by executing all replacements for a slide in one pass.
/// </summary>
public sealed class ReplaceSlideData(GateLocker gateLocker, ILogger logger) : StepBodyAsync
{
    public SlideTask Task { get; set; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;

        if (Task.TextReplacements.Count == 0 && Task.ImageReplacements.Count == 0)
        {
            logger.ForContext("TaskId", context.Workflow.Id)
                .Debug("No replacements found for row {RowIndex} in sheet {SheetName}. Skipping.", Task.RowIndex, Task.SheetTask.Identifier.SheetName);
            return ExecutionResult.Next();
        }

        logger.ForContext("TaskId", context.Workflow.Id)
            .Information("Starting data replacement for row {RowIndex} in sheet {SheetName}", Task.RowIndex, Task.SheetTask.Identifier.SheetName);

        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            if (data.OutputHandles.TryGetValue(Task.SheetTask.OutputPath, out var wrapper))
            {
                var slide = wrapper.Value.Slides[Task.RowIndex - 1];

                foreach (var shapeBase in slide.Shapes)
                {
                    if (shapeBase is not IShape shape || string.IsNullOrEmpty(shape.ShapeName))
                        continue;

                    logger.ForContext("TaskId", context.Workflow.Id)
                        .Debug("Processing shape '{ShapeName}' for row {RowIndex}", shape.ShapeName, Task.RowIndex);

                    ApplyTextReplacements(context, shape);
                    await ApplyImageReplacementsAsync(context, shape).ConfigureAwait(false);
                }

                wrapper.Save();
                
                logger.ForContext("TaskId", context.Workflow.Id)
                    .Information("Successfully replaced data for row {RowIndex} in sheet {SheetName}", Task.RowIndex, Task.SheetTask.Identifier.SheetName);
            }
        }
        catch (Exception ex)
        {
            var path = $"{Task.SheetTask.Identifier.SheetName}_{Task.RowIndex}";
            logger.ForContext("TaskId", context.Workflow.Id).ForContext("Path", path).Error(ex, "FillSlideData failed");
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        return ExecutionResult.Next();
    }

    private void ApplyTextReplacements(IStepExecutionContext context, IShape shape)
    {
        if (Task.TextReplacements.Count > 0)
        {
            logger.ForContext("TaskId", context.Workflow.Id)
                .Debug("Applying text replacements to shape '{ShapeName}' (Count: {Count})", shape.ShapeName, Task.TextReplacements.Count);
            TextComposer.Replace(shape, Task.TextReplacements);
        }
    }

    private async Task ApplyImageReplacementsAsync(IStepExecutionContext context, IShape shape)
    {
        var matchingImageTask = Task.ImageReplacements.Values.FirstOrDefault(t => 
            t.ShapeName.Equals(shape.ShapeName, StringComparison.OrdinalIgnoreCase));
            
        if (matchingImageTask != null)
        {
            var finalEditPath = matchingImageTask.EditPath + ".png";
            if (File.Exists(finalEditPath))
            {
                logger.ForContext("TaskId", context.Workflow.Id)
                    .Information("Replacing image for shape '{ShapeName}' with '{Path}'", shape.ShapeName, finalEditPath);
                
                await using var imgStream = new FileStream(finalEditPath, FileMode.Open, FileAccess.Read);
                ImageComposer.Replace(shape, imgStream);
            }
            else
            {
                logger.ForContext("TaskId", context.Workflow.Id)
                    .Warning("Edited image not found at '{Path}' for shape '{ShapeName}'", finalEditPath, shape.ShapeName);
            }
        }
    }
}
