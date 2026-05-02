using SlideGenerator.Slides.Services;
using SlideGenerator.Workflows.Generating.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Syncfusion.Presentation;

namespace SlideGenerator.Workflows.Generating.Activities;

public sealed class EditSlide(
    SfWorkbookFactory workbookFactory,
    SfPresentationRegistry presentationRegistry,
    SfTextComposer textComposer,
    SfImageComposer imageComposer,
    Setting setting) : PresentationStepBase(presentationRegistry)
{
    public RowTask RowTask { get; set; } = null!;

    protected override async Task<ExecutionResult> ExecuteStepAsync(IStepExecutionContext context)
    {
        // 1-based slide index for the specific row
        // Assuming template slide is at TemplateSlideIndex and we are cloning it for each row.
        // Workflow orchestration will handle the cloning.
        // For now, let's assume we are editing the slide at a specific index.
        var slideIndex = RowTask.TemplateSlideIndex; // This should be the index of the CLONED slide
        
        var presentationWrapper = await AcquirePresentationAsync(RowTask.OutputPath, context.CancellationToken).ConfigureAwait(false);
        var presentation = presentationWrapper.Value;

        var slide = presentation.Slides[slideIndex - 1]
                    ?? throw new InvalidOperationException($"Slide {slideIndex} does not exist.");

        var textMap = await BuildTextMapAsync(context.CancellationToken).ConfigureAwait(false);
        if (textMap is { Count: > 0 })
        {
            foreach (IShape shape in slide.Shapes)
                textComposer.Replace(shape, textMap);
        }

        if (RowTask.ResolvedInstructions is { Count: > 0 })
        {
            var downloadRoot = Path.GetFullPath(setting.Download.DownloadFolder);
            foreach (IShape shape in slide.Shapes)
            {
                var instruction = RowTask.ResolvedInstructions.FirstOrDefault(x => x.TargetShapeName == shape.ShapeName);
                if (instruction == null) continue;

                var editedPath = instruction.GetEditPath(downloadRoot, RowTask.Workbook, RowTask.WorksheetName, RowTask.RowIndex);
                if (!File.Exists(editedPath)) continue;

                await using var imageStream = new FileStream(editedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                SfImageComposer.Replace(shape, imageStream);
            }
        }

        presentationWrapper.Save();
        return ExecutionResult.Next();
    }

    private async ValueTask<IReadOnlyDictionary<string, string>> BuildTextMapAsync(CancellationToken ct)
    {
        await using var lease = await workbookFactory.AcquireAsync(RowTask.Workbook.FilePath, false, ct).ConfigureAwait(false);
        var workbook = lease.Value.Workbook;

        var ws = workbook.Worksheets[RowTask.WorksheetName];
        if (ws == null) throw new InvalidOperationException($"Worksheet '{RowTask.WorksheetName}' not found.");

        var rowContent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i <= ws.Columns.Length; i++)
        {
            var colName = ws[1, i].Value;
            if (!string.IsNullOrEmpty(colName))
                rowContent[colName] = ws[RowTask.RowIndex, i].Value;
        }

        return RowTask.TextInstructions
            .Select(general => general.Flatten(rowContent)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Value)) ?? general.Empty)
            .ToDictionary(
                x => x.Placeholder,
                x => x.Value,
                StringComparer.Ordinal);
    }
}