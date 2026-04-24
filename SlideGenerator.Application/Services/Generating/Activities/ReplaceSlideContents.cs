using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Generating.Services;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using ImageSpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;
using TextGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Texts.GeneralInstruction;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Replaces placeholders on a slide using resolved values for the current row.
/// </summary>
/// <param name="slideRegistry">The presentation file registry.</param>
/// <param name="textReplacer">The text replacement service.</param>
/// <param name="imageReplacers">The collection of image replacement services.</param>
/// <param name="workbookRegistry">The workbook file registry.</param>
/// <param name="templateSlideIndex">The 1-based index of the template slide.</param>
public sealed class ReplaceSlideContents(
    FileRegistry<IPresentation> slideRegistry,
    ITextReplacer textReplacer,
    IEnumerable<IImageReplacer> imageReplacers,
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    int templateSlideIndex) : Activity
{
    /// <inheritdoc />
    public override async ValueTask ExecuteAsync(IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var row = context.GetVariable<int>(WorksheetContextRules.Row);
        var slideIdentifier = context.GetVariable<SlideIdentifier>(WorksheetContextRules.WorkingTemplateSlide)
                                  ?.Presentation.GetSlide(templateSlideIndex + row)
                              ?? throw new ArgumentException(
                                  "Template slide must be set in context before replacing slide contents.");

        using var lease = await slideRegistry
            .AcquireAsync(slideIdentifier.Presentation.FilePath, true, cancellationToken)
            .ConfigureAwait(false);

        var presentation = lease.Value;
        var targetSlide = presentation.EnumerateSlides().ElementAtOrDefault(slideIdentifier.Index - 1)
                          ?? throw new InvalidOperationException(
                              $"Cannot replace contents: slide {slideIdentifier.Index} does not exist.");

        var textMap = await BuildTextMapAsync(context, row, cancellationToken).ConfigureAwait(false);
        if (textMap is { Count: > 0 })
            foreach (var shape in targetSlide.DescendShapes())
                textReplacer.Replace(shape, textMap);

        var imageInstructions =
            context.GetVariable<IReadOnlyDictionary<ImageSpecializedInstruction, string>>(WorksheetContextRules
                .EditedImagePaths);

        if (imageInstructions is { Count: > 0 })
            foreach (var shape in targetSlide.DescendShapes())
            {
                var pair = imageInstructions.FirstOrDefault(x => x.Key.Target.Id == shape.Id);
                if (pair.Key is null || string.IsNullOrWhiteSpace(pair.Value) || !File.Exists(pair.Value))
                    continue;

                await using var imageStream =
                    new FileStream(pair.Value, FileMode.Open, FileAccess.Read, FileShare.Read);

                foreach (var replacer in imageReplacers)
                {
                    if (imageStream.CanSeek)
                        imageStream.Position = 0;

                    if (replacer.Replace(shape, imageStream) > 0)
                        break;
                }
            }
    }

    /// <summary>
    ///     Builds the text replacement map by resolving definitions against the current data row.
    /// </summary>
    private async ValueTask<IReadOnlyDictionary<string, string>> BuildTextMapAsync(IExecutionContext context, int row,
        CancellationToken ct)
    {
        var worksheet = context.GetVariable<WorksheetIdentifier>(WorksheetContextRules.Worksheet)!;
        using var lease = await workbookRegistry
            .AcquireAsync(worksheet.Workbook.FilePath, true, ct)
            .ConfigureAwait(false);

        var workbook = lease.Value;
        if (!workbook.TryGetWorksheet(worksheet.Name, out var ws))
            throw new InvalidOperationException($"Worksheet '{worksheet.Name}' does not exist in workbook.");

        var rowContent = ws.GetRowContent(row);
        var textInstructions =
            context.GetVariable<IReadOnlyList<TextGeneralInstruction>>(WorksheetContextRules.TextInstructions) ?? [];

        return textInstructions
            .Select(x => InstructionResolver.ResolveText(x, rowContent))
            .ToDictionary(
                x => x.Placeholder,
                x => x.Value,
                StringComparer.Ordinal);
    }
}