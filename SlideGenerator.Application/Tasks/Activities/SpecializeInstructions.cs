using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Models;
using TextGeneralInstruction = SlideGenerator.Domain.Tasks.Models.Text.GeneralInstruction;
using ImageGeneralInstruction = SlideGenerator.Domain.Tasks.Models.Image.GeneralInstruction;

namespace SlideGenerator.Application.Tasks.Activities;

using SpecializedImageInstruction = Domain.Tasks.Models.Image.SpecializedInstruction;
using SpecializedTextInstruction = Domain.Tasks.Models.Text.SpecializedInstruction;

/// <summary>
///     Converts general instructions into worksheet-specific specialized instructions.
/// </summary>
/// <remarks>
///     <para>Specialization filters:</para>
///     <list type="bullet">
///         <item><description>Source worksheet must match <see cref="Worksheet"/>.</description></item>
///         <item><description>Source column must exist in the resolved worksheet instance headers.</description></item>
///         <item><description>Text placeholder must exist in <see cref="TemplatePlaceholders"/>.</description></item>
///         <item><description>Image target shape must exist in <see cref="TemplateImageShapeIds"/> and target slide must match <see cref="Slide"/>.</description></item>
///     </list>
///     <para>
///         This activity stores only lightweight specialized instruction lists in state.
///         No temporary runtime resources are persisted.
///     </para>
/// </remarks>
public sealed class SpecializeInstructions : Activity
{
    /// <summary>
    ///     Gets or sets the workbook registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IReadOnlyWorkbook> WorkbookRegistry { get; set; } = null!;

    /// <summary>
    ///     Input target worksheet identifier.
    /// </summary>
    public Input<WorksheetIdentifier> Worksheet { get; set; } = null!;

    /// <summary>
    ///     Input target template slide identifier.
    /// </summary>
    public Input<SlideIdentifier> Slide { get; set; } = null!;

    /// <summary>
    ///     Input global text instructions.
    /// </summary>
    public Input<IReadOnlyList<TextGeneralInstruction>> RawTextInstructions { get; set; } = null!;

    /// <summary>
    ///     Input global image instructions.
    /// </summary>
    public Input<IReadOnlyList<ImageGeneralInstruction>> RawImageInstructions { get; set; } = null!;

    /// <summary>
    ///     Input placeholders scanned from template slide.
    /// </summary>
    public Input<IReadOnlySet<string>> TemplatePlaceholders { get; set; } = null!;

    /// <summary>
    ///     Input image shape IDs scanned from template slide.
    /// </summary>
    public Input<IReadOnlySet<uint>> TemplateImageShapeIds { get; set; } = null!;

    /// <summary>
    ///     Output worksheet-specific text instructions.
    /// </summary>
    public Output<IReadOnlyList<SpecializedTextInstruction>> TextInstructions { get; set; } = null!;

    /// <summary>
    ///     Output worksheet-specific image instructions.
    /// </summary>
    public Output<IReadOnlyList<SpecializedImageInstruction>> ImageInstructions { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var worksheet = context.Get(Worksheet);
        var slide = context.Get(Slide);
        var rawText = context.Get(RawTextInstructions) ?? [];
        var rawImage = context.Get(RawImageInstructions) ?? [];
        var placeholders = context.Get(TemplatePlaceholders) ?? new HashSet<string>(StringComparer.Ordinal);
        var imageShapeIds = context.Get(TemplateImageShapeIds) ?? new HashSet<uint>();

        if (worksheet is null || slide is null)
        {
            context.Set(TextInstructions, []);
            context.Set(ImageInstructions, []);
            return ValueTask.CompletedTask;
        }

        var workbook = WorkbookRegistry.GetOrOpen(worksheet.Workbook.FilePath, isEditable: false);
        if (!workbook.TryGetWorksheet(worksheet.Name, out var worksheetInstance))
        {
            context.Set(TextInstructions, []);
            context.Set(ImageInstructions, []);
            return ValueTask.CompletedTask;
        }

        var headers = worksheetInstance.GetHeadersName();

        var headerSet = headers.ToHashSet(StringComparer.Ordinal);

        var textInstructions = rawText
            .Where(instruction => placeholders.Contains(instruction.Placeholder))
            .SelectMany(instruction => instruction.Flatten(instruction))
            .Where(instruction => Equals(instruction.Source.Worksheet, worksheet))
            .Where(instruction => headerSet.Contains(instruction.Source.ColumnName))
            .ToList();

        var imageInstructions = rawImage
            .Where(instruction => Equals(instruction.Target.Slide, slide))
            .Where(instruction => imageShapeIds.Contains(instruction.Target.Id))
            .SelectMany(instruction => instruction.Flatten(instruction))
            .Where(instruction => Equals(instruction.Source.Worksheet, worksheet))
            .Where(instruction => headerSet.Contains(instruction.Source.ColumnName))
            .ToList();

        context.Set(TextInstructions, textInstructions);
        context.Set(ImageInstructions, imageInstructions);
        return ValueTask.CompletedTask;
    }
}