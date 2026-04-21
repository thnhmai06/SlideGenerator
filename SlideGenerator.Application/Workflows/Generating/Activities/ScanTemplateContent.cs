using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Scans one template slide and returns placeholders and image shape identifiers used for specialization.
/// </summary>
public sealed class ScanTemplateContent(
    FileRegistry<IPresentation> slideRegistry,
    ITextReplacer textReplacer) : Activity
{
    /// <summary>Input target slide descriptor in working presentation.</summary>
    public required Input<SlideIdentifier> TemplateSlide { get; init; }

    /// <summary>Output set of mustache placeholder names found in the template slide.</summary>
    public Output<IReadOnlySet<string>> Placeholders { get; init; } = null!;

    /// <summary>Output set of image shape IDs found in the template slide.</summary>
    public Output<IReadOnlySet<uint>> ImageShapeIds { get; init; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var slideIdentifier = context.Get(TemplateSlide);
        if (slideIdentifier is null)
            throw new ArgumentException("Template slide must be provided.");

        using var lease = await slideRegistry
            .AcquireAsync(slideIdentifier.Presentation.FilePath, true, context.CancellationToken)
            .ConfigureAwait(false);

        var presentation = lease.Value;
        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIdentifier.Index - 1);
        if (targetSlide == null)
            throw new InvalidOperationException(
                $"Cannot scan template content: slide {slideIdentifier.Index} does not exist.");

        var shapes = targetSlide.DescendShapes().ToList();
        context.Set(Placeholders, shapes.SelectMany(textReplacer.Scan).ToHashSet(StringComparer.Ordinal));
        context.Set(ImageShapeIds, shapes
            .Where(s => s.IsPicture || s.HasBlipFill)
            .Select(s => s.Id)
            .ToHashSet());
    }
}
