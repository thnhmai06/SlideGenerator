using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Resources;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SlideGenerator.Domain.Workflows.Models.Generating.Images;
using TextGeneralInstruction = SlideGenerator.Domain.Workflows.Models.Generating.Texts.GeneralInstruction;
using ImageGeneralInstruction = SlideGenerator.Domain.Workflows.Models.Generating.Images.GeneralInstruction;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

using SpecializedImageInstruction = SpecializedInstruction;
using SpecializedTextInstruction = Domain.Workflows.Models.Generating.Texts.SpecializedInstruction;

/// <summary>
///     Converts general instructions into worksheet-specific specialized instructions.
/// </summary>
/// <remarks>
///     <para>Specialization filters:</para>
///     <list type="bullet">
///         <item><description>Source worksheet must match <see cref="Worksheet"/>.</description></item>
///         <item><description>Source column must exist in the resolved worksheet instance headers.</description></item>
///         <item><description>HasTextFrame placeholder must exist in <see cref="TemplatePlaceholders"/>.</description></item>
///         <item><description>Image target shape must exist in <see cref="TemplateImageShapeIds"/> and target slide must match <see cref="TemplateSlide"/>.</description></item>
///     </list>
///     <para>
///         This activity stores only lightweight specialized instruction lists in state.
///         No temporary runtime resources are persisted.
///     </para>
/// </remarks>
public sealed class SpecializeInstructions(Registry<IReadOnlyWorkbook> workbookRegistry) : Activity
{
    /// <summary>
    ///     Input target worksheet identifier.
    /// </summary>
    public required Input<WorksheetIdentifier> Worksheet { get; init; }

    /// <summary>
    ///     Input target template slide identifier.
    /// </summary>
    public required Input<SlideIdentifier> TemplateSlide { get; init; }

    /// <summary>
    ///     Input global text instructions.
    /// </summary>
    public required Input<IReadOnlyList<TextGeneralInstruction>> RawTextInstructions { get; init; }

    /// <summary>
    ///     Input global image instructions.
    /// </summary>
    public required Input<IReadOnlyList<ImageGeneralInstruction>> RawImageInstructions { get; init; }

    /// <summary>
    ///     Input placeholders scanned from template slide.
    /// </summary>
    public required Input<IReadOnlySet<string>> TemplatePlaceholders { get; init; }

    /// <summary>
    ///     Input image shape IDs scanned from template slide.
    /// </summary>
    public required Input<IReadOnlySet<uint>> TemplateImageShapeIds { get; init; }

    /// <summary>
    ///     Output worksheet-specific text instructions.
    /// </summary>
    public Output<IReadOnlyList<SpecializedTextInstruction>> TextInstructions { get; init; } = null!;

    /// <summary>
    ///     Output worksheet-specific image instructions.
    /// </summary>
    public Output<IReadOnlyList<SpecializedImageInstruction>> ImageInstructions { get; init; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var worksheetIdentifier = context.Get(Worksheet);
        var templateSlideIdentifier = context.Get(TemplateSlide);
        var rawTexts = context.Get(RawTextInstructions) ?? [];
        var rawImages = context.Get(RawImageInstructions) ?? [];
        var placeholders = context.Get(TemplatePlaceholders) ?? new HashSet<string>(StringComparer.Ordinal);
        var imageShapeIds = context.Get(TemplateImageShapeIds) ?? new HashSet<uint>();

        if (worksheetIdentifier is null || templateSlideIdentifier is null)
            throw new ArgumentException("Worksheet and template slide must be provided.");

        using var workbookLease = workbookRegistry.Acquire(worksheetIdentifier.Workbook.FilePath, true);
        var workbook = workbookLease.Value;
        if (!workbook.TryGetWorksheet(worksheetIdentifier.Name, out var worksheetInstance))
            throw new InvalidOperationException(
                $"Cannot specialize instructions: worksheet '{worksheetIdentifier.Name}' does not exist in workbook.");

        var headers = worksheetInstance.Headers;
        var headerSet = headers.ToHashSet(StringComparer.Ordinal);

        var textInstructions = rawTexts
            .Where(instruction => placeholders.Contains(instruction.Placeholder))
            .SelectMany(instruction => instruction.Flatten(instruction))
            .Where(instruction => Equals(instruction.Source.Worksheet, worksheetIdentifier))
            .Where(instruction => headerSet.Contains(instruction.Source.Name))
            .ToList();

        var imageInstructions = rawImages
            .Where(instruction => Equals(instruction.Target.Slide, templateSlideIdentifier))
            .Where(instruction => imageShapeIds.Contains(instruction.Target.Id))
            .SelectMany(instruction => instruction.Flatten(instruction))
            .Where(instruction => Equals(instruction.Source.Worksheet, worksheetIdentifier))
            .Where(instruction => headerSet.Contains(instruction.Source.Name))
            .ToList();

        context.Set(TextInstructions, textInstructions);
        context.Set(ImageInstructions, imageInstructions);
        return ValueTask.CompletedTask;
    }
}