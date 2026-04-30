using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Scanning.Workflows.Activities;

/// <summary>
///     Scans a single presentation file and stores the result in the
///     <see cref="ScanningVariables.PresentationSummaries" /> Variable.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="ScanningVariables.PresentationItem" /> (default) — the presentation identifier to
///     scan.<br />
///     <b>Variables written:</b> <see cref="ScanningVariables.PresentationSummaries" /> — adds the scan result
///     entry.<br />
///     <b>Services:</b> <see cref="FileRegistry{IPresentation}" /> — acquires a shared read lease; released on completion.
///     <br />
///     <see cref="ITextComposer" />.<br />
///     <b>Logging:</b> via <c>context.State.Logger</c>.<br />
///     <b>CancellationToken:</b> propagated to lease acquire.
/// </remarks>
public sealed class ScanPresentation(
    FileRegistry<IPresentation> presentationRegistry,
    ITextComposer textComposer,
    Variable<PresentationIdentifier> presentationVar) : ILeafActivity<object>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<object> context)
    {
        var fullPath = Path.GetFullPath(context.GetVariable(presentationVar).FilePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Presentation file not found.", fullPath);

        await using var lease = await presentationRegistry.AcquireAsync(fullPath, false, context.CancellationToken)
            .ConfigureAwait(false);
        var presentation = lease.Value;

        var slides = new List<SlideSummary>();
        foreach (var slide in presentation.EnumerateSlides())
        {
            var shapes = slide.DescendShapes().ToList();

            var placeholders = shapes
                .SelectMany(textComposer.Scan)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var imageShapePreviews = shapes
                .Where(shape => shape.IsPicture || shape.HasBlipFill)
                .Select(shape => shape.GetPreview())
                .ToList();

            var preview = await slide.GetPreview(skipPreview: false, ct: context.CancellationToken)
                .ConfigureAwait(false);

            slides.Add(new SlideSummary(slide.Index, slide.Id, slide.Name ?? string.Empty, preview, placeholders, imageShapePreviews));
        }

        context.GetVariable(ScanningVariables.PresentationSummaries)[fullPath] =
            new PresentationSummary(fullPath, slides);
    }
}
