using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Services.Scanning.Workflows.Activities;

/// <summary>
///     A workflow activity that scans a presentation file to extract slide summaries, placeholders, and image shape previews.
/// </summary>
/// <remarks>
///     The scanning process includes:
///     <list type="bullet">
///         <item>
///             <description>Acquiring a read-only lease on the presentation file.</description>
///         </item>
///         <item>
///             <description>Enumerating all slides and descending into their shape trees.</description>
///         </item>
///         <item>
///             <description>Identifying text placeholders (e.g., {{VariableName}}) using the <see cref="ITextComposer"/>.</description>
///         </item>
///         <item>
///             <description>Capturing preview images for the slide itself and any picture/blip-filled shapes.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="presentationRegistry">The registry used to manage concurrent access to presentation files.</param>
/// <param name="textComposer">The composer used to scan text placeholders within slides.</param>
public sealed class ScanPresentation(
    FileRegistry<IPresentation> presentationRegistry,
    ITextComposer textComposer) : StepBodyAsync
{
    /// <summary>
    ///     Gets or sets the identifier of the presentation to be scanned.
    /// </summary>
    public PresentationIdentifier Presentation { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the resulting summary of the scanned presentation.
    /// </summary>
    public PresentationSummary Result { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the exception if the scan failed.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <inheritdoc />
    /// <exception cref="FileNotFoundException">Thrown when the presentation file does not exist at the specified path.</exception>
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            var fullPath = Path.GetFullPath(Presentation.FilePath);

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

                slides.Add(new SlideSummary(slide.Index, slide.Id, slide.Name ?? string.Empty, preview, placeholders,
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
