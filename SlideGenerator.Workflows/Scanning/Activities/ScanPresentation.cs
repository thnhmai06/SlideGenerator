using SlideGenerator.Slides.Services;
using SlideGenerator.Workflows.Scanning.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Syncfusion.Presentation;

namespace SlideGenerator.Workflows.Scanning.Activities;

public sealed class ScanPresentation(
    SfPresentationRegistry presentationRegistry,
    SfTextComposer textComposer) : StepBodyAsync
{
    public string PresentationFilePath { get; set; } = null!;
    public PresentationSummary Result { get; set; } = null!;
    public Exception? Exception { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            var fullPath = Path.GetFullPath(PresentationFilePath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Presentation not found.", fullPath);

            await using var lease = await presentationRegistry.AcquireAsync(fullPath, false, context.CancellationToken).ConfigureAwait(false);
            var presentation = lease.Value.Presentation;

            var slides = new List<SlideSummary>();
            for (var i = 0; i < presentation.Slides.Count; i++)
            {
                var slide = presentation.Slides[i];
                var shapes = slide.Shapes.Cast<IShape>().ToList();

                var placeholders = shapes
                    .SelectMany(textComposer.Scan)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                var imageShapePreviews = shapes
                    .Where(shape => shape is IPicture || shape.Fill.FillType == FillType.Picture)
                    .Select(shape => new ShapePreview(
                        (uint)(shape.ShapeName?.GetHashCode() ?? 0),
                        shape.ShapeName ?? string.Empty,
                        shape.GetBounds(),
                        shape.GetShapePreview()))
                    .ToList();

                var preview = new SlidePreview(i + 1, slide.GetSlidePreview());

                slides.Add(new SlideSummary(i + 1, (uint)i + 1, slide.Name ?? string.Empty, preview, placeholders,
                    imageShapePreviews));
            }

            Result = new PresentationSummary(fullPath, slides);
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        return ExecutionResult.Next();
    }
}