using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using ImageGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Images.GeneralInstruction;
using ImageSpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;
using TextGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Texts.GeneralInstruction;
using TextSpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Texts.SpecializedInstruction;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Specializes general instructions for a specific worksheet.
/// </summary>
/// <remarks>
///     Filters instructions based on matching worksheet, column headers, and template content.
/// </remarks>
/// <param name="workbookRegistry">The workbook file registry for acquiring workbook access.</param>
/// <param name="rawTextInstructions">The raw list of general text replacement instructions.</param>
/// <param name="rawImageInstructions">The raw list of general image replacement instructions.</param>
public sealed class SpecializeInstructions(
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    IReadOnlyList<TextGeneralInstruction> rawTextInstructions,
    IReadOnlyList<ImageGeneralInstruction> rawImageInstructions) : Activity
{
    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown if the template slide is missing in context.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the targeted worksheet does not exist in the workbook.</exception>
    public override async ValueTask ExecuteAsync(IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var worksheetIdentifier = context.GetVariable<WorksheetIdentifier>(WorksheetContextRules.Worksheet)!;
        var templateSlideIdentifier = context.GetVariable<SlideIdentifier>(WorksheetContextRules.WorkingTemplateSlide)
                                      ?? throw new ArgumentException(
                                          "Template slide must be set in context before specializing.");
        var placeholders = context.GetVariable<IReadOnlySet<string>>(WorksheetContextRules.TemplatePlaceholders) ??
                           new HashSet<string>();
        var imageShapeIds = context.GetVariable<IReadOnlySet<uint>>(WorksheetContextRules.TemplateImageShapeIds) ??
                            new HashSet<uint>();

        using var workbookLease = await workbookRegistry
            .AcquireAsync(worksheetIdentifier.Workbook.FilePath, false, cancellationToken)
            .ConfigureAwait(false);
        var workbook = workbookLease.Value;
        if (!workbook.TryGetWorksheet(worksheetIdentifier.Name, out var worksheetInstance))
            throw new InvalidOperationException(
                $"Cannot specialize instructions: worksheet '{worksheetIdentifier.Name}' does not exist in workbook.");

        var headerSet = worksheetInstance.Headers.ToHashSet(StringComparer.Ordinal);

        context.SetVariable<IReadOnlyList<TextSpecializedInstruction>>(WorksheetContextRules.TextInstructions,
            rawTextInstructions
                .Where(instruction => placeholders.Contains(instruction.Placeholder))
                .SelectMany(instruction => instruction.Flatten(instruction))
                .Where(instruction => Equals(instruction.Source.Worksheet, worksheetIdentifier))
                .Where(instruction => headerSet.Contains(instruction.Source.Name))
                .ToList());

        context.SetVariable<IReadOnlyList<ImageSpecializedInstruction>>(WorksheetContextRules.ImageInstructions,
            rawImageInstructions
                .Where(instruction => Equals(instruction.Target.Slide, templateSlideIdentifier))
                .Where(instruction => imageShapeIds.Contains(instruction.Target.Id))
                .SelectMany(instruction => instruction.Flatten(instruction))
                .Where(instruction => Equals(instruction.Source.Worksheet, worksheetIdentifier))
                .Where(instruction => headerSet.Contains(instruction.Source.Name))
                .ToList());
    }
}