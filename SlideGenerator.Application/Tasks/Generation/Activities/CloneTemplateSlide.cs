using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Application.Tasks.Generation.Activities;

/// <summary>
///     Clones a source slide inside presentation and inserts it at a target 1-based position.
/// </summary>
/// <remarks>
///     <para>
///         The activity resolves target presentation through registry by file path.
///     </para>
/// </remarks>
public sealed class CloneTemplateSlide(IRegistry<IPresentation> slideRegistry) : Activity
{
    /// <summary>
    ///    Path to presentation file where slide should be cloned.
    /// </summary>
    public required Input<SlideIdentifier> TemplateSlide { get; init; }

    /// <summary>
    ///     1-based index where cloned slide should be inserted.
    /// </summary>
    public required Input<int> InsertAtIndex { get; init; }

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var templateSlideIdentifier = context.Get(TemplateSlide);
        var insertAtIndex = context.Get(InsertAtIndex);
        if (templateSlideIdentifier is null || insertAtIndex < 1)
            throw new InvalidOperationException("template slide identifier and insert index must be valid.");

        var presentation = slideRegistry.GetOrOpen(templateSlideIdentifier.Presentation.FilePath, isEditable: true);
        if (presentation.EnumerateSlides().ElementAtOrDefault(templateSlideIdentifier.Index - 1) == null)
            throw new InvalidOperationException(
                $"Cannot clone slide {templateSlideIdentifier.Index}: source slide does not exist.");

        _ = presentation.CopySlide(templateSlideIdentifier.Index, insertAtIndex);
        return ValueTask.CompletedTask;
    }
}