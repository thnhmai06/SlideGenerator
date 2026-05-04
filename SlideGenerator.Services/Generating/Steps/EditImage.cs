using System.Drawing;
using ImageMagick;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Images;
using SlideGenerator.Images.Services;
using SlideGenerator.Services.Generating.Workflows.Models;
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
        var data = (GeneratingTask)context.Workflow.Data;
        var finalEditPath = Task.EditPath + ".png";

        // Idempotency: skip if the file already exists
        if (File.Exists(finalEditPath)) return ExecutionResult.Next();
        
        // Skip if SourceUri is null and no FallbackImagePath is provided
        if (Task.SourceUri == null && string.IsNullOrWhiteSpace(Task.FallbackImagePath))
            return ExecutionResult.Next();

        // Discover the source file
        string? sourceFile = null;
        var downloadDir = Path.GetDirectoryName(Task.DownloadPath);
        var downloadPrefix = Path.GetFileName(Task.DownloadPath);
        
        if (downloadDir != null && Directory.Exists(downloadDir))
        {
            sourceFile = Directory.GetFiles(downloadDir, $"{downloadPrefix}.*").FirstOrDefault();
        }

        // Use fallback if a primary source is missing
        if (sourceFile == null || !File.Exists(sourceFile))
        {
            if (!string.IsNullOrWhiteSpace(Task.FallbackImagePath) && File.Exists(Task.FallbackImagePath))
            {
                sourceFile = Task.FallbackImagePath;
            }
            else
            {
                // Source is missing and no fallback available
                if (Task.SourceUri != null)
                {
                    data.Errors.TryAdd($"Edit_MissingSource_{downloadPrefix}",
                        new FileNotFoundException("Source image and fallback not found for editing.", Task.DownloadPath));
                }
                return ExecutionResult.Next();
            }
        }

        // Ensure target directory exists
        var editDir = Path.GetDirectoryName(finalEditPath);
        if (editDir != null && !Directory.Exists(editDir)) Directory.CreateDirectory(editDir);

        await gateLocker.AcquireAsync(GateType.EditImage).ConfigureAwait(false);
        try
        {
            using var image = new MagickImage(sourceFile);
            var targetSize = new Size((int)Math.Round(Task.Width), (int)Math.Round(Task.Height));

            // 1. Calculate ROI based on the selected algorithm
            var roi = await roiResolver.CalculateRoiAsync(
                image,
                targetSize,
                Task.EditOptions.RoiOption).ConfigureAwait(false);

            // 2. Crop the image to the ROI
            image.Crop(new MagickGeometry(roi.X, roi.Y, (uint)roi.Width, (uint)roi.Height));

            // 3. Resize with a maintained aspect ratio to fit the target shape dimensions
            var currentSize = new Size((int)image.Width, (int)image.Height);
            var maxAspectSize = currentSize.GetMaxAspectSize(targetSize);
            image.Resize(new MagickGeometry((uint)maxAspectSize.Width, (uint)maxAspectSize.Height));

            // 4. Save the edited image as PNG
            await image.WriteAsync(finalEditPath).ConfigureAwait(false);

            // Optional: Delete raw download image to save space (only if it was the primary source)
            if (data.Request.DeleteDownloadImage && sourceFile != Task.FallbackImagePath)
            {
                try { File.Delete(sourceFile); } catch { /* ignore */ }
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
