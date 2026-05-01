using SlideGenerator.Application.Modules.Images.Abstractions;
using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Images.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Size = System.Drawing.Size;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     A workflow activity that processes a downloaded image to fit its target slide shape using ROI calculations.
/// </summary>
/// <remarks>
///     The editing process includes:
///     <list type="bullet">
///         <item>
///             <description>Resolving the target shape's physical dimensions from the presentation.</description>
///         </item>
///         <item>
///             <description>Decoding the source image and calculating the optimal Region of Interest (ROI) based on the specified <see cref="RoiOption"/>.</description>
///         </item>
///         <item>
///             <description>Applying crops or transformations to the image to match the slide's aspect ratio and size.</description>
///         </item>
///         <item>
///             <description>Fallback: If advanced processing fails, the original image is copied to the edit path to avoid breaking the pipeline.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="imageDecoder">Service to decode image bytes into processing-friendly matrices.</param>
/// <param name="roiCalculator">Service to perform computer vision-based ROI detection (e.g., face detection).</param>
/// <param name="slideRegistry">Registry to access slide metadata for target dimension resolution.</param>
/// <param name="settingProvider">Provider for global application settings.</param>
public sealed class EditImage(
    IImageDecoder imageDecoder,
    IRoiCalculator roiCalculator,
    FileRegistry<IPresentation> slideRegistry,
    ISettingProvider settingProvider) : StepBodyAsync
{
    /// <summary>
    ///     Gets or sets the row-specific task context containing the image instruction and the local path of the downloaded source.
    /// </summary>
    public RowTask RowTask { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the exception if the image editing process encountered a critical error.
    /// </summary>
    public Exception? Exception { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var (instruction, downloadedPath) = RowTask.EditItem
                                            ?? throw new ArgumentException("EditItem must be set on the RowTask.");

        if (string.IsNullOrWhiteSpace(downloadedPath) || !File.Exists(downloadedPath))
            return ExecutionResult.Next();

        var editedPath = instruction.GetEditPath(settingProvider.Current.Download.DownloadFolder, RowTask.Worksheet,
            RowTask.RowIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(editedPath)!);

        try
        {
            var targetSize = await ResolveTargetSizeAsync(slideRegistry, instruction, context.CancellationToken)
                .ConfigureAwait(false);
            await ProcessWithRoiAsync(imageDecoder, roiCalculator, downloadedPath, editedPath, targetSize, instruction.Edit.RoiOption)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Exception = ex;
            File.Copy(downloadedPath, editedPath, true);
        }
        
        return ExecutionResult.Next();
    }

    private async ValueTask ProcessWithRoiAsync(IImageDecoder decoder, IRoiCalculator calculator, string sourcePath,
        string destinationPath, Size targetSize, RoiOption roiOption)
    {
        var sourceBytes = await File.ReadAllBytesAsync(sourcePath).ConfigureAwait(false);
        using var sourceMat = decoder.Decode(sourceBytes);
        if (sourceMat.Empty())
            throw new InvalidOperationException($"Cannot decode image '{sourcePath}'.");

        var normalizedSize = new Size(
            Math.Max(1, targetSize.Width > 0 ? targetSize.Width : sourceMat.Width),
            Math.Max(1, targetSize.Height > 0 ? targetSize.Height : sourceMat.Height));

        await calculator.CalculateRoiAsync(sourceMat, normalizedSize, roiOption.Type, roiOption)
            .ConfigureAwait(false);
        await File.WriteAllBytesAsync(destinationPath, sourceMat.ToByteArray()).ConfigureAwait(false);
    }

    private async ValueTask<Size> ResolveTargetSizeAsync(
        FileRegistry<IPresentation> registry,
        SpecializedInstruction instruction,
        CancellationToken ct)
    {
        await using var lease = await registry
            .AcquireAsync(instruction.Target.Slide.Presentation.FilePath, false, ct)
            .ConfigureAwait(false);
        var slide = lease.Value
                        .EnumerateSlides()
                        .ElementAtOrDefault(instruction.Target.Slide.Index - 1)
                    ?? throw new InvalidOperationException(
                        $"Slide '{instruction.Target.Slide.Index}' does not exist.");

        var shape = slide
                        .DescendShapes()
                        .FirstOrDefault(x => x.Id == instruction.Target.Id)
                    ?? throw new InvalidOperationException(
                        $"Shape '{instruction.Target.Id}' does not exist in slide '{instruction.Target.Slide.Index}'.");

        return shape.Bounds.Size.ToSize();
    }
}
