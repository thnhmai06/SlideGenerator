using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Resources;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Application.Workflows.Generating.Models.Images;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Replaces both text and image contents on a target slide in sequence.
/// </summary>
/// <remarks>
///     This activity executes two internal tasks in order: <c>ReplaceTexts</c>, then <c>ReplaceImages</c>.
/// </remarks>
public sealed class ReplaceSlideContents(
    Registry<IPresentation> slideRegistry,
    ITextReplacer textReplacer,
    IEnumerable<IImageReplacer> imageReplacers) : Activity
{
    /// <summary>
    ///     Identifier of target slide to replace contents on.
    ///     /
    /// </summary>
    public required Input<SlideIdentifier> SlideIdentifier { get; init; }

    /// <summary>
    ///     Replacement values by placeholder key (without braces).
    /// </summary>
    public required Input<IReadOnlyDictionary<string, string>> TextInstructions { get; init; }

    /// <summary>
    ///     Assignments from specialized instruction to local image file path.
    /// </summary>
    public required Input<IReadOnlyDictionary<SpecializedInstruction, string>> ImageInstructions { get; init; }

    /// <summary>
    ///     Output count of text changes.
    /// </summary>
    public Output<int> ReplacedTextCount { get; init; } = null!;

    /// <summary>
    ///     Output count of image replacements.
    /// </summary>
    public Output<int> ReplacedImageCount { get; init; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var slideIdentifier = context.Get(SlideIdentifier);
        if (slideIdentifier is null)
            throw new ArgumentException("Presentation path and slide index must be valid.");

        var presentation = slideRegistry.GetOrOpen(slideIdentifier.Presentation.FilePath, true);
        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIdentifier.Index - 1)
                          ?? throw new InvalidOperationException(
                              $"Cannot replace contents: slide {slideIdentifier.Index} does not exist.");

        var textInstructions = context.Get(TextInstructions);
        if (textInstructions != null)
        {
            var replacedTextCount = 0;
            if (textInstructions.Count > 0)
                replacedTextCount += targetSlide.DescendShapes()
                    .Sum(shape => textReplacer.Replace(shape, textInstructions));
            context.Set(ReplacedTextCount, replacedTextCount);
        }

        var imageInstructions = context.Get(ImageInstructions);
        if (imageInstructions != null)
        {
            var replacedImageCount = 0;
            if (imageInstructions.Count > 0)
                foreach (var shape in targetSlide.DescendShapes())
                {
                    var imagePair = imageInstructions.FirstOrDefault(x => x.Key.Target.Id == shape.Id);
                    if (imagePair.Key is null || string.IsNullOrWhiteSpace(imagePair.Value) ||
                        !File.Exists(imagePair.Value))
                        continue;

                    using var imageStream = new FileStream(imagePair.Value, FileMode.Open, FileAccess.Read,
                        FileShare.Read);

                    foreach (var imageReplacer in imageReplacers)
                    {
                        if (imageStream.CanSeek)
                            imageStream.Position = 0;

                        var replaced = imageReplacer.Replace(shape, imageStream);
                        if (replaced <= 0)
                            continue;

                        replacedImageCount += replaced;
                        break;
                    }
                }

            context.Set(ReplacedImageCount, replacedImageCount);
        }

        return ValueTask.CompletedTask;
    }
}