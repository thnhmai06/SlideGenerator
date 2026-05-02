using System.Drawing;
using SlideGenerator.Images.Models.Options;
using SlideGenerator.Workflows.Generating.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Syncfusion.Presentation;

namespace SlideGenerator.Workflows.Generating.Activities;

public sealed class EditImage(
    IImageDecoder imageDecoder,
    IRoiCalculator roiCalculator,
    SfPresentationRegistry slideRegistry,
    Setting setting) : StepBodyAsync
{
    public RowTask RowTask { get; set; } = null!;
    public Exception? Exception { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        if (RowTask.EditItem == null)
            throw new ArgumentException("EditItem must be set on the RowTask.");

        var instruction = RowTask.EditItem.Value.Key;
        var downloadedPath = RowTask.EditItem.Value.Value;

        if (string.IsNullOrWhiteSpace(downloadedPath) || !File.Exists(downloadedPath))
            return ExecutionResult.Next();

        var editedPath = instruction.GetEditPath(setting.Download.DownloadFolder, RowTask.Workbook, RowTask.WorksheetName, RowTask.RowIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(editedPath)!);

        try
        {
            var targetSize = await ResolveTargetSizeAsync(instruction, context.CancellationToken).ConfigureAwait(false);
            await ProcessWithRoiAsync(downloadedPath, editedPath, targetSize, instruction.Edit.RoiOption).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Exception = ex;
            File.Copy(downloadedPath, editedPath, true);
        }
        
        return ExecutionResult.Next();
    }

    private async ValueTask ProcessWithRoiAsync(string sourcePath, string destinationPath, Size targetSize, RoiOption roiOption)
    {
        var sourceBytes = await File.ReadAllBytesAsync(sourcePath).ConfigureAwait(false);
        using var sourceMat = imageDecoder.Decode(sourceBytes);
        if (sourceMat.Empty())
            throw new InvalidOperationException($"Cannot decode image '{sourcePath}'.");

        var normalizedSize = new Size(
            Math.Max(1, targetSize.Width > 0 ? targetSize.Width : sourceMat.Width),
            Math.Max(1, targetSize.Height > 0 ? targetSize.Height : sourceMat.Height));

        await roiCalculator.CalculateRoiAsync(sourceMat, normalizedSize, roiOption.Type, roiOption).ConfigureAwait(false);
        
        // Use OpenCV to encode and save
        sourceMat.SaveImage(destinationPath);
    }

    private async ValueTask<Size> ResolveTargetSizeAsync(SpecializedInstruction instruction, CancellationToken ct)
    {
        await using var lease = await slideRegistry.AcquireAsync(Path.GetFullPath(RowTask.OutputPath), false, ct).ConfigureAwait(false);
        var presentation = lease.Value.Presentation;
        
        var slide = presentation.Slides[RowTask.TemplateSlideIndex - 1];
        var shape = slide.Shapes.Cast<IShape>().FirstOrDefault(x => x.ShapeName == instruction.TargetShapeName)
                    ?? throw new InvalidOperationException($"Shape '{instruction.TargetShapeName}' not found in slide {RowTask.TemplateSlideIndex}.");

        return new Size((int)Math.Round(shape.Width), (int)Math.Round(shape.Height));
    }
}