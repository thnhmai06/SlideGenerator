using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>Replaces placeholders on a slide.</summary>
/// <remarks>Updates both text and image content for the current row.</remarks>
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
    /// <exception cref="ArgumentException">Thrown if template slide is missing in context.</exception>
    /// <exception cref="InvalidOperationException">Thrown if target slide does not exist.</exception>
    public override async ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var row = context.GetVariable<int>(WorksheetContextRules.Row);
        var slideIdentifier = context.GetVariable<SlideIdentifier>(WorksheetContextRules.WorkingTemplateSlide)?.Presentation.GetSlide(templateSlideIndex + row)
                              ?? throw new ArgumentException("Template slide must be set in context before replacing slide contents.");

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

        var imageInstructions = context.GetVariable<IReadOnlyDictionary<SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction, string>>(WorksheetContextRules.EditedImagePaths);
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

    /// <summary>Builds the text replacement map for the current row.</summary>
    /// <param name="context">The execution context.</param>
    /// <param name="row">The current row index.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A dictionary of placeholders and their replacement values.</returns>
    private async ValueTask<IReadOnlyDictionary<string, string>?> BuildTextMapAsync(IExecutionContext context, int row, CancellationToken ct)
    {
        var worksheet = context.GetVariable<WorksheetIdentifier>(WorksheetContextRules.Worksheet)!;
        using var lease = await workbookRegistry
            .AcquireAsync(worksheet.Workbook.FilePath, true, ct)
            .ConfigureAwait(false);

        var workbook = lease.Value;
        if (!workbook.TryGetWorksheet(worksheet.Name, out var ws))
            throw new InvalidOperationException($"Worksheet '{worksheet.Name}' does not exist in workbook.");

        var rowContent = ws.GetRowContent(row);
        var textInstructions = context.GetVariable<IReadOnlyList<SlideGenerator.Application.Services.Generating.Models.Texts.SpecializedInstruction>>(WorksheetContextRules.TextInstructions) ?? [];
        return textInstructions
            .GroupBy(x => x.Placeholder, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => rowContent.TryGetValue(g.First().Source.Name, out var v) ? v : string.Empty,
                StringComparer.Ordinal);
    }
}
