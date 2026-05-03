using System.Drawing;
using ImageMagick;
using SlideGenerator.Gate.Models;
using SlideGenerator.Gate.Services;
using SlideGenerator.Images;
using SlideGenerator.Images.Services;
using SlideGenerator.Services.Generating.Workflows;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Services.Generating.Steps;

/// <summary>
///     Processes a single image by cropping and resizing it to match
///     the target shape dimensions using an intelligent ROI algorithm.
/// </summary>
public sealed class EditImage(
    RoiResolver roiResolver,
    GateLocker gateLocker) : StepBodyAsync
{
    /// <summary>
    ///     The editing task to process.
    ///     Mapped from the ForEach loop in the workflow.
    /// </summary>
    public ImageTask Task { get; set; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingData)context.Workflow.Data;
        var finalEditPath = Task.EditPath + ".png";

        // Idempotency: skip if the file already exists
        if (File.Exists(finalEditPath)) return ExecutionResult.Next();
        
        // Skip if SourceUri is null (already logged warning in DownloadImage)
        if (Task.SourceUri == null) return ExecutionResult.Next();

        // Discover the downloaded file based on its expected prefix (since Downloader handles extension)
        var downloadDir = Path.GetDirectoryName(Task.DownloadPath);
        var downloadPrefix = Path.GetFileName(Task.DownloadPath);
        
        if (downloadDir == null || !Directory.Exists(downloadDir))
        {
            data.Errors.TryAdd($"Edit_MissingSourceDir_{downloadPrefix}", 
                new DirectoryNotFoundException($"Download directory not found: {downloadDir}"));
            return ExecutionResult.Next();
        }

        var downloadedFile = Directory.GetFiles(downloadDir, $"{downloadPrefix}.*").FirstOrDefault();

        if (downloadedFile == null)
        {
            data.Errors.TryAdd($"Edit_MissingSourceFile_{downloadPrefix}",
                new FileNotFoundException("Source image not found for editing.", Task.DownloadPath));
            return ExecutionResult.Next();
        }

        // Ensure directory exists
        var editDir = Path.GetDirectoryName(finalEditPath);
        if (editDir != null && !Directory.Exists(editDir)) Directory.CreateDirectory(editDir);

        await gateLocker.AcquireAsync(GateType.EditImage).ConfigureAwait(false);
        try
        {
            using var image = new MagickImage(downloadedFile);
            var targetSize = new Size((int)Math.Round(Task.Width), (int)Math.Round(Task.Height));

            // 1. Calculate ROI based on the selected algorithm
            var roi = await roiResolver.CalculateRoiAsync(
                image,
                targetSize,
                Task.EditOptions.RoiOption.Type,
                Task.EditOptions.RoiOption).ConfigureAwait(false);

            // 2. Crop the image to the ROI
            image.Crop(new MagickGeometry(roi.X, roi.Y, (uint)roi.Width, (uint)roi.Height));

            // 3. Resize with maintained aspect ratios to fit the target shape dimensions
            var currentSize = new Size((int)image.Width, (int)image.Height);
            var maxAspectSize = currentSize.GetMaxAspectSize(targetSize);
            image.Resize(new MagickGeometry((uint)maxAspectSize.Width, (uint)maxAspectSize.Height));

            // 4. Save the edited image as PNG
            await image.WriteAsync(finalEditPath).ConfigureAwait(false);

            // Optional: Delete raw download image to save space
            if (data.Request.DeleteDownloadImage)
            {
                try { File.Delete(downloadedFile); } catch { /* ignore */ }
            }
        }
        catch (Exception ex)
        {
            data.Errors.TryAdd($"Edit_{Path.GetFileName(finalEditPath)}", ex);
        }
        finally
        {
            gateLocker.Release(GateType.EditImage);
        }

        return ExecutionResult.Next();
    }
}
