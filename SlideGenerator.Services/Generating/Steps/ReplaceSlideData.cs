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
public sealed class ReplaceSlideData(GateLocker gateLocker) : StepBodyAsync
{
    public SlideTask Task { get; set; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;

        if (Task.TextReplacements.Count == 0 && Task.ImageReplacements.Count == 0)
            return ExecutionResult.Next();

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

                    ApplyTextReplacements(shape);
                    await ApplyImageReplacementsAsync(shape).ConfigureAwait(false);
                }

                wrapper.Save();
            }
        }
        catch (Exception ex)
        {
            data.Errors.TryAdd($"FillSlideData_{Task.SheetTask.Identifier.SheetName}_{Task.RowIndex}", ex);
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        return ExecutionResult.Next();
    }

    private void ApplyTextReplacements(IShape shape)
    {
        if (Task.TextReplacements.Count > 0)
            TextComposer.Replace(shape, Task.TextReplacements);
    }

    private async Task ApplyImageReplacementsAsync(IShape shape)
    {
        var matchingImageTask = Task.ImageReplacements.Values.FirstOrDefault(t => 
            t.ShapeName.Equals(shape.ShapeName, StringComparison.OrdinalIgnoreCase));
            
        if (matchingImageTask != null)
        {
            var finalEditPath = matchingImageTask.EditPath + ".png";
            if (File.Exists(finalEditPath))
            {
                await using var imgStream = new FileStream(finalEditPath, FileMode.Open, FileAccess.Read);
                ImageComposer.Replace(shape, imgStream);
            }
        }
    }
}
