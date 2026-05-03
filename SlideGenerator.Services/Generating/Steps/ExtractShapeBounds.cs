using SlideGenerator.Gate.Models;
using SlideGenerator.Gate.Services;
using SlideGenerator.Services.Generating.Workflows;
using SlideGenerator.Slides;
using SlideGenerator.Slides.Entities;
using Syncfusion.Presentation;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Services.Generating.Steps;

public sealed class ExtractShapeBounds(GateLocker gateLocker) : StepBodyAsync
{
    public ShapeTask Task { get; set; } = null!;
    
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingData)context.Workflow.Data;
        await gateLocker.AcquireAsync(GateType.ReadPresentation).ConfigureAwait(false);
        try
        {
            using var wrapper = new SfPresentation(Task.Worksheet.OutputPresentationPath, false);
            if (wrapper.Value.Slides[0].Shapes[Task.ShapeIndex] is IShape shape)
            {
                var shapeId = (uint)(shape.ShapeName?.GetHashCode() ?? 0);
                data.ShapeBounds.TryAdd(shapeId, shape.GetBoundsF());
            }
        }
        finally
        {
            gateLocker.Release(GateType.ReadPresentation);
        }
        return ExecutionResult.Next();
    }
}
