using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Slide.Entities.Presentation;
using SlideGenerator.Domain.Slide.Models.Identifiers;

namespace SlideGenerator.Application.Tasks.Generation.Activities;

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
public sealed class ScanTemplateContent(
    IRegistry<IPresentation> slideRegistry,
    ITextReplacer textReplacer) : Activity
{
    /// <summary>
    ///     Input target slide descriptor in working presentation.
    /// </summary>
    public required Input<SlideIdentifier> TemplateSlide { get; init; }

    /// <summary>
    ///     Output set of mustache placeholder names found in the template slide.
    /// </summary>
    public Output<IReadOnlySet<string>> Placeholders { get; init; } = null!;

    /// <summary>
    ///     Output set of image shape IDs found in the template slide.
    /// </summary>
    public Output<IReadOnlySet<uint>> ImageShapeIds { get; init; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var slideIdentifier = context.Get(TemplateSlide);
        if (slideIdentifier is null)
            throw new ArgumentException("Template slide must be provided.");

        var presentation = slideRegistry.GetOrOpen(slideIdentifier.Presentation.FilePath, true);
        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIdentifier.Index - 1);
        if (targetSlide == null)
            throw new InvalidOperationException(
                $"Cannot scan template content: slide {slideIdentifier.Index} does not exist.");

        var shapes = targetSlide.DescendShapes().ToList();
        var placeholders = shapes
            .SelectMany(textReplacer.Scan);
        var imageShapeIds = shapes
            .Where(shape => shape.IsPicture || shape.HasBlipFill)
            .Select(shape => shape.Id)
            .ToHashSet();

        context.Set(Placeholders, placeholders);
        context.Set(ImageShapeIds, imageShapeIds);
        return ValueTask.CompletedTask;
    }
}