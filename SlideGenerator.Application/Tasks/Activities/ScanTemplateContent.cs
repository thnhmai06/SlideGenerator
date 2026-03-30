using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Scans one template slide and returns placeholders and image shape identifiers used for specialization.
/// </summary>
/// <remarks>
///     <para>
///         Reads an opened working presentation from registry and scans one slide.
///     </para>
///     <list type="bullet">
///         <item><description>Writes lightweight sets: <see cref="Placeholders"/> and <see cref="ImageShapeIds"/>.</description></item>
///         <item><description>Does not persist OpenXml document objects in workflow state.</description></item>
///     </list>
/// </remarks>
public sealed class ScanTemplateContent : Activity
{
    /// <summary>
    ///     Input target slide descriptor in working presentation.
    /// </summary>
    public Input<SlideIdentifier> SlideInfo { get; set; } = null!;

    /// <summary>
    ///     Output set of mustache placeholder names found in the template slide.
    /// </summary>
    public Output<IReadOnlySet<string>> Placeholders { get; set; } = null!;

    /// <summary>
    ///     Output set of image shape IDs found in the template slide.
    /// </summary>
    public Output<IReadOnlySet<uint>> ImageShapeIds { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the slide registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IPresentation> SlideRegistry { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the slide content operator dependency (injected by DI).
    /// </summary>
    public ISlideContentOperator SlideContentOperator { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var slideInfo = context.Get(SlideInfo);
        var presentationPath = slideInfo?.Presentation.FilePath;
        var slideIndex = slideInfo?.Index ?? 0;

        if (string.IsNullOrWhiteSpace(presentationPath) || slideIndex <= 0)
            throw new ArgumentException("Slide info is invalid.");

        var presentation = SlideRegistry.GetOrOpen(presentationPath, isEditable: true);
        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIndex - 1);
        if (targetSlide == null)
            throw new InvalidOperationException($"Cannot scan template content: slide {slideIndex} does not exist.");

        var (placeholders, imageShapeIds) = SlideContentOperator.ScanTemplateContent(targetSlide);

        context.Set(Placeholders, placeholders);
        context.Set(ImageShapeIds, imageShapeIds);
        return ValueTask.CompletedTask;
    }
}