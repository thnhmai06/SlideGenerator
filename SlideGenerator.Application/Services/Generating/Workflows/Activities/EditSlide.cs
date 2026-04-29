using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.Rules;
using SlideGenerator.Application.Services.Generating.Models.States;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Replaces Mustache text placeholders and image shapes on the cloned row slide with
///     values and edited images for the current row.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.RowItem" />,
///     <see cref="VariablesDeclaration.WorkingTemplateSlide" />,
///     <see cref="VariablesDeclaration.RowTextInstructions" />,
///     <see cref="VariablesDeclaration.SpecializedInstructions" />,
///     <see cref="WorksheetContext.PresentationLease" />.<br />
///     <b>Services:</b> <see cref="FileRegistry{IReadOnlyWorkbook}" />, <c>ITextComposer</c>,
///     <c>IImageComposer</c>, <c>ISettingProvider</c>.<br />
///     <b>Logging:</b> errors propagated as exceptions (caller's <c>TryNode</c> logs them).<br />
///     <b>CancellationToken:</b> not required (leases already open).
/// </remarks>
public sealed class EditSlide(
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    ITextComposer textComposer,
    IImageComposer imageComposer,
    ISettingProvider settingProvider,
    Variable<RowIdentifier> rowVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var rc = context.GetVariable(rowVar);
        var slideIdentifier = context.GetVariable(VariablesDeclaration.WorkingTemplateSlide)
                                  ?.Presentation.GetSlide(WorkflowConstants.WorkingTemplateSlideIndex + rc.Index)
                              ?? throw new ArgumentException(
                                  "Template slide must be set in context before replacing slide contents.");

        var presentation = CloneTemplateSlide.GetWorksheetSnapshot(context).Context.PresentationLease!.Value;

        var slide = presentation.EnumerateSlides().ElementAtOrDefault(slideIdentifier.Index - 1)
                    ?? throw new InvalidOperationException(
                        $"Cannot replace contents: slide {slideIdentifier.Index} does not exist.");

        var textMap = await BuildTextMapAsync(context, rc.Index).ConfigureAwait(false);
        if (textMap is { Count: > 0 })
            foreach (var shape in slide.DescendShapes())
                textComposer.Replace(shape, textMap);

        var rowInstructions = context.GetVariable(VariablesDeclaration.SpecializedInstructions);
        if (rowInstructions is { Count: > 0 })
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
        IActivityContext<WorkflowTask> context, int row)
    {
        var worksheet = context.GetVariable(VariablesDeclaration.WorksheetItem);

        await using var lease = await workbookRegistry
            .AcquireAsync(worksheet.Workbook.FilePath, false, context.CancellationToken)
            .ConfigureAwait(false);
        var workbook = lease.Value;

        if (!workbook.TryGetWorksheet(worksheet.Name, out var ws))
            throw new InvalidOperationException(
                $"Worksheet '{worksheet.Name}' does not exist in workbook.");

        var rowContent = ws.GetRowContent(row);
        var textInstructions = context.GetVariable(VariablesDeclaration.RowTextInstructions);

        return textInstructions
            .Select(general => general.Flatten(general, rowContent)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Value)) ?? general.Empty)
            .ToDictionary(
                x => x.Placeholder,
                x => x.Value,
                StringComparer.Ordinal);
    }
}
