using System.Drawing;
using ImageMagick;
using SlideGenerator.Images.Models.Options;
using SlideGenerator.Images.Services;
using SlideGenerator.Workflows.Generating.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Syncfusion.Presentation;

namespace SlideGenerator.Workflows.Generating.Activities;

public sealed class EditImage(
    RoiResolver roiCalculator,
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
        using var sourceMagickImage = SlideGenerator.Images.Utilities.Decode(sourceBytes);
        
        var normalizedSize = new Size(
            Math.Max(1, targetSize.Width > 0 ? targetSize.Width : sourceMagickImage.Width),
            Math.Max(1, targetSize.Height > 0 ? targetSize.Height : sourceMagickImage.Height));

        var roi = await roiCalculator.CalculateRoiAsync(sourceMagickImage, normalizedSize, roiOption.Type, roiOption).ConfigureAwait(false);
        
        // Crop the image to ROI
        var croppedImage = SlideGenerator.Images.Utilities.Crop(sourceMagickImage, roi);
        
        try
        {
            // Resize if needed
            if (croppedImage.Width != normalizedSize.Width || croppedImage.Height != normalizedSize.Height)
            {
                var resizedImage = SlideGenerator.Images.Utilities.Resize(croppedImage, normalizedSize);
                try
                {
                    resizedImage.Write(destinationPath);
                }
                finally
                {
                    resizedImage.Dispose();
                }
            }
            else
            {
                croppedImage.Write(destinationPath);
            }
        }
        finally
        {
            croppedImage.Dispose();
        }
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