using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using ImageGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Images.GeneralInstruction;
using TextGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Texts.GeneralInstruction;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Filters and stores general instruction definitions in the context for per-row resolution.
/// </summary>
/// <param name="workbookRegistry">The workbook file registry.</param>
/// <param name="rawTextInstructions">The raw list of general text instructions.</param>
/// <param name="rawImageInstructions">The raw list of general image instructions.</param>
public sealed class SpecializeInstructions(
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    IReadOnlyList<TextGeneralInstruction> rawTextInstructions,
    IReadOnlyList<ImageGeneralInstruction> rawImageInstructions) : Activity
{
    /// <inheritdoc />
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

        // Filter and store the GENERAL instructions (definitions) themselves
        context.SetVariable<IReadOnlyList<TextGeneralInstruction>>(WorksheetContextRules.TextInstructions,
            rawTextInstructions
                .Where(instruction => placeholders.Contains(instruction.Placeholder))
                .Where(instruction =>
                    instruction.Sources.Any(s =>
                        Equals(s.Worksheet, worksheetIdentifier) && headerSet.Contains(s.Name)))
                .ToList());

        context.SetVariable<IReadOnlyList<ImageGeneralInstruction>>(WorksheetContextRules.ImageInstructions,
            rawImageInstructions
                .Where(instruction => Equals(instruction.Target.Slide, templateSlideIdentifier))
                .Where(instruction => imageShapeIds.Contains(instruction.Target.Id))
                .Where(instruction =>
                    instruction.Sources.Any(s =>
                        Equals(s.Worksheet, worksheetIdentifier) && headerSet.Contains(s.Name)))
                .ToList());
    }
}