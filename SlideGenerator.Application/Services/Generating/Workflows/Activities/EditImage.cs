using SlideGenerator.Application.Modules.Images.Abstractions;
using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Images.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using Size = System.Drawing.Size;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Crops and resizes the downloaded image to fit the target shape dimensions using ROI detection
///     (center or rule-of-thirds), writing the result to the edit output path.
///     Falls back to a plain file copy if processing fails.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.RowTaskItem" />.<br />
///     <b>Services:</b> <c>IImageDecoder</c>, <c>IRoiCalculator</c>,
///     <see cref="FileRegistry{IPresentation}" /> (to read target shape bounds),
///     <c>ISettingProvider</c> (for <c>DownloadFolder</c>).<br />
///     <b>CancellationToken:</b> propagated to registry acquire.
/// </remarks>
public sealed class EditImage(
    IImageDecoder imageDecoder,
    IRoiCalculator roiCalculator,
    FileRegistry<IPresentation> slideRegistry,
    ISettingProvider settingProvider,
    Variable<RowTask> rowTaskVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var task = context.GetVariable(rowTaskVar);
        var (instruction, downloadedPath) = task.EditItem
                                            ?? throw new ArgumentException("EditItem must be set on the RowTask.");

        if (string.IsNullOrWhiteSpace(downloadedPath) || !File.Exists(downloadedPath))
            return;

        var editedPath = instruction.GetEditPath(settingProvider.Current.Download.DownloadFolder, task.Worksheet,
            task.RowIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(editedPath)!);

        try
        {
            var targetSize = await ResolveTargetSizeAsync(instruction, context.CancellationToken)
                .ConfigureAwait(false);
            await ProcessWithRoiAsync(downloadedPath, editedPath, targetSize, instruction.Edit.RoiOption)
                .ConfigureAwait(false);
        }
        catch
        {
            File.Copy(downloadedPath, editedPath, true);
        }
    }

    private async ValueTask ProcessWithRoiAsync(string sourcePath,
        string destinationPath, Size targetSize, RoiOption roiOption)
    {
        var sourceBytes = await File.ReadAllBytesAsync(sourcePath).ConfigureAwait(false);
        using var sourceMat = imageDecoder.Decode(sourceBytes);
        if (sourceMat.Empty())
            throw new InvalidOperationException($"Cannot decode image '{sourcePath}'.");

        var normalizedSize = new Size(
            Math.Max(1, targetSize.Width > 0 ? targetSize.Width : sourceMat.Width),
            Math.Max(1, targetSize.Height > 0 ? targetSize.Height : sourceMat.Height));

        await roiCalculator.CalculateRoiAsync(sourceMat, normalizedSize, roiOption.Type, roiOption)
            .ConfigureAwait(false);
        await File.WriteAllBytesAsync(destinationPath, sourceMat.ToByteArray()).ConfigureAwait(false);
    }

    private async ValueTask<Size> ResolveTargetSizeAsync(
        SpecializedInstruction instruction,
        CancellationToken ct)
    {
        await using var lease = await slideRegistry
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