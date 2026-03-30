using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Slide.Entities;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Replaces both text and image contents on a target slide in sequence.
/// </summary>
/// <remarks>
///     This activity executes two internal tasks in order: <c>ReplaceTexts</c>, then <c>ReplaceImages</c>.
/// </remarks>
public sealed class ReplaceSlideContents : Activity
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

    /// <summary>
    ///     Gets or sets the slide registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IPresentation> SlideRegistry { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the slide content operator dependency (injected by DI).
    /// </summary>
    public ISlideContentOperator ContentOperator { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var presentationPath = context.Get(PresentationPath);
        var slideIndex = context.Get(SlideIndex);
        if (slideIndex <= 0 || string.IsNullOrWhiteSpace(presentationPath))
            throw new ArgumentException("Presentation path and slide index must be valid.");

        var presentation = SlideRegistry.GetOrOpen(presentationPath, isEditable: true);
        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIndex - 1)
                          ?? throw new InvalidOperationException(
                              $"Cannot replace contents: slide {slideIndex} does not exist.");

        var textInstructions = context.Get(TextInstructions) ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var replacedTextCount = textInstructions.Count == 0
            ? 0
            : ContentOperator.ReplaceText(targetSlide, textInstructions);
        context.Set(ReplacedTextCount, replacedTextCount);

        var imageInstructions = context.Get(ImageInstructions) ?? new Dictionary<uint, string>();
        var replacedImageCount = imageInstructions.Count == 0
            ? 0
            : ContentOperator.ReplaceImages(targetSlide, imageInstructions);
        context.Set(ReplacedImageCount, replacedImageCount);

        return ValueTask.CompletedTask;
    }
}
