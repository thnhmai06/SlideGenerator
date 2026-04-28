using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Application.Workflows.DSL;
using SlideGenerator.Application.Workflows.Rules;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Replaces Mustache text placeholders and image shapes on the cloned row slide with
///     values and edited images for the current row.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.RowItem" />.<br/>
///     <b>Data read:</b> <see cref="SheetTask.WorkingTemplateSlide" />, <see cref="SheetTask.RowTextInstructions" />,
///     <see cref="SheetTask.RowSpecializedInstructions" /> (entry for the current row — edited file path derived via
///     <see cref="SpecializedInstruction.GetEditPath" /> and checked with <see cref="File.Exists" />).<br/>
///     <b>Services:</b> <see cref="FileRegistry{IPresentation}" />, <see cref="FileRegistry{IReadOnlyWorkbook}" />,
///     <c>ITextComposer</c>, <c>IImageComposer</c>, <c>ISettingProvider</c>.<br/>
///     <b>Logging:</b> errors propagated as exceptions (caller's <c>TryNode</c> logs them).<br/>
///     <b>CancellationToken:</b> propagated to both registries acquired.
/// </remarks>
public sealed class EditSlide(
    FileRegistry<IPresentation> slideRegistry,
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    ITextComposer textComposer,
    IImageComposer imageComposer,
    ISettingProvider settingProvider,
    Variable<RowIdentifier> rowVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var data = context.Data;
        var rc = context.GetVariable(rowVar);
        var sheetTask = data.SheetTasks[rc.Worksheet];

        var slideIdentifier = sheetTask.WorkingTemplateSlide
                                  ?.Presentation.GetSlide(WorkflowConstants.WorkingTemplateSlideIndex + rc.Index)
                              ?? throw new ArgumentException(
                                  "Template slide must be set in context before replacing slide contents.");

        using var lease = await slideRegistry
            .AcquireAsync(slideIdentifier.Presentation.FilePath, true, context.CancellationToken)
            .ConfigureAwait(false);

        var presentation = lease.Value;
        var slide = presentation.EnumerateSlides().ElementAtOrDefault(slideIdentifier.Index - 1)
                          ?? throw new InvalidOperationException(
                              $"Cannot replace contents: slide {slideIdentifier.Index} does not exist.");

        var textMap = await BuildTextMapAsync(sheetTask, rc.Index, context.CancellationToken)
            .ConfigureAwait(false);
        if (textMap is { Count: > 0 })
            foreach (var shape in slide.DescendShapes())
                textComposer.Replace(shape, textMap);

        var rowInstructions = sheetTask.RowSpecializedInstructions.GetValueOrDefault(rc.Index) ?? [];
        if (rowInstructions.Count > 0)
        {
            var downloadRoot = Path.GetFullPath(settingProvider.Current.Download.DownloadFolder);
            foreach (var shape in slide.DescendShapes())
            {
                var instruction = rowInstructions.FirstOrDefault(x => x.Target.Id == shape.Id);
                if (instruction == null) continue;

                var editedPath = instruction.GetEditPath(downloadRoot, rc.Worksheet, rc.Index);
                if (!File.Exists(editedPath)) continue;

                await using var imageStream =
                    new FileStream(editedPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (imageStream.CanSeek)
                    imageStream.Position = 0;

                imageComposer.Replace(shape, imageStream);
            }
        }
    }

    private async ValueTask<IReadOnlyDictionary<string, string>> BuildTextMapAsync(
        SheetTask sheetTask, int row, CancellationToken ct)
    {
        using var lease = await workbookRegistry
            .AcquireAsync(sheetTask.Identifier.Workbook.FilePath, true, ct)
            .ConfigureAwait(false);

        var workbook = lease.Value;
        if (!workbook.TryGetWorksheet(sheetTask.Identifier.Name, out var ws))
            throw new InvalidOperationException(
                $"Worksheet '{sheetTask.Identifier.Name}' does not exist in workbook.");

        var rowContent = ws.GetRowContent(row);
        var textInstructions = sheetTask.RowTextInstructions;

        return textInstructions
            .Select(general => general.Flatten(general, rowContent)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Value)) ?? general.Empty)
            .ToDictionary(
                x => x.Placeholder,
                x => x.Value,
                StringComparer.Ordinal);
    }
}
