using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Scans one template slide and writes placeholder names and image shape IDs into the worksheet context.
/// </summary>
public sealed class ScanTemplateContent(
    FileRegistry<IPresentation> slideRegistry,
    ITextReplacer textReplacer) : Activity
{
    /// <inheritdoc />
    public override async ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var slideIdentifier = context.GetVariable<SlideIdentifier>(WorksheetContextRules.WorkingTemplateSlide)
                              ?? throw new ArgumentException("Template slide must be set in context before scanning.");

        using var lease = await slideRegistry
            .AcquireAsync(slideIdentifier.Presentation.FilePath, true, cancellationToken)
            .ConfigureAwait(false);

        var presentation = lease.Value;
        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIdentifier.Index - 1);
        if (targetSlide == null)
            throw new InvalidOperationException(
                $"Cannot scan template content: slide {slideIdentifier.Index} does not exist.");

        var shapes = targetSlide.DescendShapes().ToList();
        context.SetVariable(WorksheetContextRules.TemplatePlaceholders, shapes.SelectMany(textReplacer.Scan).ToHashSet(StringComparer.Ordinal));
        context.SetVariable(WorksheetContextRules.TemplateImageShapeIds, shapes
            .Where(s => s.IsPicture || s.HasBlipFill)
            .Select(s => s.Id)
            .ToHashSet());
    }
}
