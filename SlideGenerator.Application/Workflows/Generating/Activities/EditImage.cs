using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Images.Abstractions;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Workflows.Generating.Models.Images;
using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Images.Models;
using SlideGenerator.Domain.Images.Rules;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using Size = System.Drawing.Size;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Edits one downloaded image item and saves the processed result to local storage.
///     Slot acquisition is handled externally (e.g., via <see cref="AcquireSlot" />).
/// </summary>
public sealed class EditImage(
    FileRegistry<IPresentation> slideRegistry,
    IRoiCalculator roiCalculator,
    IImageDecoder imageDecoder,
    ISettingProvider settingProvider) : Activity
{
    public required Input<KeyValuePair<SpecializedInstruction, string>> Item { get; init; }
    public required Input<int> RowIndex { get; init; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var rowIndex = context.Get(RowIndex);
        if (rowIndex <= 0)
            throw new ArgumentException("Row index must be greater than 0.");

        var (instruction, downloadedPath) = context.Get(Item);
        if (string.IsNullOrWhiteSpace(downloadedPath) || !File.Exists(downloadedPath))
            return;

        var editedPath = instruction.GetEditPath(settingProvider.Current.Download.DownloadFolder, rowIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(editedPath)!);

        try
        {
            var targetSize = await ResolveTargetSizeAsync(instruction, context.CancellationToken).ConfigureAwait(false);
            await ProcessWithRoiAsync(downloadedPath, editedPath, targetSize, instruction.Edit.RoiOption)
                .ConfigureAwait(false);
        }
        catch
        {
            File.Copy(downloadedPath, editedPath, true);
        }
    }

    private async ValueTask ProcessWithRoiAsync(string sourcePath, string destinationPath, Size targetSize, RoiOption roiOption)
    {
        var sourceBytes = await File.ReadAllBytesAsync(sourcePath).ConfigureAwait(false);
        using var sourceMat = imageDecoder.Decode(sourceBytes);
        if (sourceMat.Empty())
            throw new InvalidOperationException($"Cannot decode image '{sourcePath}'.");

        var normalizedSize = new Size(
            targetSize.Width > 0 ? targetSize.Width : sourceMat.Width,
            targetSize.Height > 0 ? targetSize.Height : sourceMat.Height);
        normalizedSize = new Size(Math.Max(1, normalizedSize.Width), Math.Max(1, normalizedSize.Height));

        var roiType = roiOption switch
        {
            CenterOption => RoiType.Center,
            RuleOfThirdsOption => RoiType.RuleOfThirds,
            _ => throw new NotSupportedException($"ROI option type '{roiOption.GetType().Name}' is not supported.")
        };

        await roiCalculator.CalculateRoiAsync(sourceMat, normalizedSize, roiType, roiOption).ConfigureAwait(false);
        await File.WriteAllBytesAsync(destinationPath, sourceMat.ToByteArray()).ConfigureAwait(false);
    }

    private async ValueTask<Size> ResolveTargetSizeAsync(SpecializedInstruction instruction, CancellationToken ct)
    {
        using var lease = await slideRegistry
            .AcquireAsync(instruction.Target.Slide.Presentation.FilePath, true, ct)
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

        return new Size(
            Math.Max(1, (int)Math.Round(shape.Bounds.Width, MidpointRounding.AwayFromZero)),
            Math.Max(1, (int)Math.Round(shape.Bounds.Height, MidpointRounding.AwayFromZero)));
    }
}
