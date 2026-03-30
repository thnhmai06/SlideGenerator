using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Slide.Entities;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Replaces image contents on a target slide by shape identifier.
/// </summary>
/// <remarks>
///     <para>
///         Inputs map shape ID to local image file path. Missing files or missing shapes are skipped safely.
///     </para>
///     <para>
///         The activity updates presentation content only; save is handled by a separate activity.
///     </para>
/// </remarks>
public sealed class ReplaceSlideImages(IRegistry<IPresentation> slideRegistry, ISlideContentOperator contentOperator) : Activity
{
    /// <summary>
    ///     Full path of target presentation.
    /// </summary>
    public Input<string> PresentationPath { get; set; } = null!;

    /// <summary>
    ///     1-based slide index in target presentation.
    /// </summary>
    public Input<int> SlideIndex { get; set; } = null!;

    /// <summary>
    ///     Assignments from shape ID to local image file path.
    /// </summary>
    public Input<IReadOnlyDictionary<uint, string>> Assignments { get; set; } = null!;

    /// <summary>
    ///     Output count of successfully replaced shapes.
    /// </summary>
    public Output<int> ReplacedCount { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var presentationPath = context.Get(PresentationPath);
        var slideIndex = context.Get(SlideIndex);
        var assignments = context.Get(Assignments) ?? new Dictionary<uint, string>();

        if (slideIndex <= 0 || string.IsNullOrWhiteSpace(presentationPath))
            throw new ArgumentException("Presentation path and slide index must be valid.");

        if (assignments.Count == 0)
        {
            context.Set(ReplacedCount, 0);
            return ValueTask.CompletedTask;
        }

        var presentation = slideRegistry.GetOrOpen(presentationPath, isEditable: true);

        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIndex - 1);
        if (targetSlide == null)
            throw new InvalidOperationException($"Cannot replace images: slide {slideIndex} does not exist.");

        var replaced = contentOperator.ReplaceImages(targetSlide, assignments);

        context.Set(ReplacedCount, replaced);
        return ValueTask.CompletedTask;
    }
}