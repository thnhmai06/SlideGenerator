using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Slide.Entities;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Replaces mustache placeholders with row values on a target slide.
/// </summary>
/// <remarks>
///     Missing values keep original placeholders unchanged.
/// </remarks>
public sealed class ReplaceSlideText(IRegistry<IPresentation> slideRegistry, ISlideContentOperator contentOperator) : Activity
{
    /// <summary>
    ///     Full path of target presentation.
    /// </summary>
    public Input<string> PresentationPath { get; set; } = null!;

    /// <summary>
    ///     1-based slide index in current transient presentation.
    /// </summary>
    public Input<int> SlideIndex { get; set; } = null!;

    /// <summary>
    ///     Replacement values by placeholder key (without braces).
    /// </summary>
    public Input<IReadOnlyDictionary<string, string>> Replacements { get; set; } = null!;

    /// <summary>
    ///     Output indicating whether any text content changed (non-zero if changes made).
    /// </summary>
    public Output<int> Changed { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var replacements = context.Get(Replacements) ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var presentationPath = context.Get(PresentationPath);
        var slideIndex = context.Get(SlideIndex);
        if (slideIndex <= 0 || string.IsNullOrWhiteSpace(presentationPath))
            throw new ArgumentException("Presentation path and slide index must be valid.");

        if (replacements.Count == 0)
        {
            context.Set(Changed, 0);
            return ValueTask.CompletedTask;
        }

        var presentation = slideRegistry.GetOrOpen(presentationPath, isEditable: true);

        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIndex - 1);
        if (targetSlide == null)
            throw new InvalidOperationException($"Cannot replace text: slide {slideIndex} does not exist.");

        var changeCount = contentOperator.ReplaceText(targetSlide, replacements);

        context.Set(Changed, changeCount);
        return ValueTask.CompletedTask;
    }
}