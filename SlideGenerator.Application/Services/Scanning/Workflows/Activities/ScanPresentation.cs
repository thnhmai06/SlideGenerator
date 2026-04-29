using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Workflows;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SlideGenerator.Domain.Slides.Models.Previews;

namespace SlideGenerator.Application.Services.Scanning.Workflows.Activities;

/// <summary>
///     Scans a single presentation file and stores the result in the
///     <see cref="VariablesDeclaration.PresentationSummaries" /> Variable.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.PresentationItem" /> — the presentation identifier to
///     scan.<br />
///     <b>Variables written:</b> <see cref="VariablesDeclaration.PresentationSummaries" /> — adds the scan result
///     entry.<br />
///     <b>Services:</b> <see cref="FileRegistry{IPresentation}" /> — acquires a shared read lease; released on completion.<br />
///     <see cref="ITextComposer" />, <see cref="IImageComposer" />.<br />
///     <b>Logging:</b> via <c>context.State.Logger</c>.<br />
///     <b>CancellationToken:</b> propagated to lease acquire.
/// </remarks>
public sealed class ScanPresentation(
    FileRegistry<IPresentation> presentationRegistry,
    ITextComposer textComposer,
    IImageComposer imageComposer,
    Variable<PresentationIdentifier> presentationVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var fullPath = Path.GetFullPath(context.GetVariable(presentationVar).FilePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Presentation file not found.", fullPath);

        await using var lease = await presentationRegistry.AcquireAsync(fullPath, false, context.CancellationToken)
            .ConfigureAwait(false);
        var presentation = lease.Value;

        var slides = presentation.EnumerateSlides()
            .Select((slide, index) =>
            {
                var shapes = slide.DescendShapes().ToList();
                var placeholders = shapes
                    .SelectMany(textComposer.Scan)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                var imageShapePreviews = shapes
                    .Where(shape => shape.IsPicture || shape.HasBlipFill)
                    .Select(shape =>
                    {
                        var image = imageComposer.Scan(shape) ?? [];
                        return new ShapePreview(shape.Id, shape.Name, shape.Bounds, image);
                    })
                    .ToList();

                var slidePreview = new SlidePreview(
                    index + 1,
                    slide.Id,
                    slide.Name ?? string.Empty,
                    imageShapePreviews.FirstOrDefault()?.Image ?? []);

                return new SlideSummary(index + 1, slidePreview, placeholders, imageShapePreviews);
            })
            .ToList();

        context.GetVariable(VariablesDeclaration.PresentationSummaries)[fullPath] =
            new PresentationSummary(fullPath, slides);
    }
}
