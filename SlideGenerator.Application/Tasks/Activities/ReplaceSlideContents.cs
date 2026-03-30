using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Replaces both text and image contents on a target slide in sequence.
/// </summary>
/// <remarks>
///     This activity executes two internal tasks in order: <c>ReplaceTexts</c>, then <c>ReplaceImages</c>.
/// </remarks>
public sealed class ReplaceSlideContents(
    IRegistry<IPresentation> slideRegistry,
    ISlideContentOperator contentOperator) : Activity
{
    /// <summary>
    ///     Identifier of target slide to replace contents on.
    /// / </summary>
    public Input<SlideIdentifier> SlideIdentifier { get; set; } = null!;

    /// <summary>
    ///     Replacement values by placeholder key (without braces).
    /// </summary>
    public Input<IReadOnlyDictionary<string, string>> TextInstructions { get; set; } = null!;

    /// <summary>
    ///     Assignments from shape ID to local image file path.
    /// </summary>
    public Input<IReadOnlyDictionary<uint, string>> ImageInstructions { get; set; } = null!;

    /// <summary>
    ///     Output count of text changes.
    /// </summary>
    public Output<int> ReplacedTextCount { get; set; } = null!;

    /// <summary>
    ///     Output count of image replacements.
    /// </summary>
    public Output<int> ReplacedImageCount { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var slideIdentifier = context.Get(SlideIdentifier);
        if (slideIdentifier is null)
            throw new ArgumentException("Presentation path and slide index must be valid.");

        var presentation = slideRegistry.GetOrOpen(slideIdentifier.Presentation.FilePath, isEditable: true);
        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIdentifier.Index - 1)
                          ?? throw new InvalidOperationException(
                              $"Cannot replace contents: slide {slideIdentifier.Index} does not exist.");

        var textInstructions = context.Get(TextInstructions);
        if (textInstructions != null)
        {
            var replacedTextCount = textInstructions.Count == 0
                ? 0
                : contentOperator.ReplaceText(targetSlide, textInstructions);
            context.Set(ReplacedTextCount, replacedTextCount);
        }

        var imageInstructions = context.Get(ImageInstructions);
        if (imageInstructions != null)
        {
            var replacedImageCount = imageInstructions.Count == 0
                ? 0
                : contentOperator.ReplaceImages(targetSlide, imageInstructions);
            context.Set(ReplacedImageCount, replacedImageCount);
        }

        return ValueTask.CompletedTask;
    }
}