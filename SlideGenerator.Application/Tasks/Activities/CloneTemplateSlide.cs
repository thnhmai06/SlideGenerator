using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Slide.Entities;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Clones a source slide inside presentation and inserts it at a target 1-based position.
/// </summary>
/// <remarks>
///     <para>
///         The activity resolves target presentation through registry by file path.
///     </para>
/// </remarks>
public sealed class CloneTemplateSlide : Activity
{
    /// <summary>
    ///     Full path of target presentation.
    /// </summary>
    public Input<string> PresentationPath { get; set; } = null!;

    /// <summary>
    ///     1-based index of source slide to clone.
    /// </summary>
    public Input<int> SourceSlideIndex { get; set; } = null!;

    /// <summary>
    ///     1-based index where cloned slide should be inserted.
    /// </summary>
    public Input<int> InsertAtIndex { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the slide registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IPresentation> SlideRegistry { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var presentationPath = context.Get(PresentationPath);
        var sourceSlideIndex = context.Get(SourceSlideIndex);
        var insertAtIndex = context.Get(InsertAtIndex);
        if (sourceSlideIndex <= 0 || string.IsNullOrWhiteSpace(presentationPath))
            throw new ArgumentException("Presentation path and source slide index must be valid.");

        var presentation = SlideRegistry.GetOrOpen(presentationPath, isEditable: true);
        if (presentation.EnumerateSlides().ElementAtOrDefault(sourceSlideIndex - 1) == null)
            throw new InvalidOperationException($"Cannot clone slide {sourceSlideIndex}: source slide does not exist.");

        _ = presentation.CopySlide(sourceSlideIndex, insertAtIndex);
        return ValueTask.CompletedTask;
    }
}