using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Configs;
using SlideGenerator.Application.Download.Contracts;
using SlideGenerator.Application.Image.Contracts;
using SlideGenerator.Application.Slide.Contracts;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Framework.Slide;
using SlideGenerator.Infrastructure.Base;
using SlideGenerator.Infrastructure.Utilities;
using DrawingPicture = DocumentFormat.OpenXml.Drawing.Picture;
using PresentationShape = DocumentFormat.OpenXml.Presentation.Shape;
using Presentation = SlideGenerator.Framework.Slide.Models.Presentation;

namespace SlideGenerator.Infrastructure.Slide.Services;

using RowContent = Dictionary<string, string?>;
using ReplaceInstructions = Dictionary<string, string>;

public class SlideServices(
    ILogger<SlideServices> logger,
    IDownloadService downloadService,
    IImageService imageService,
    SlideWorkingManager slideWorkingManager) : Service(logger), ISlideServices
{
    public async Task ProcessRowAsync(
        string outputPath,
        string templatePath,
        TextConfig[] textConfigs,
        ImageConfig[] imageConfigs,
        RowContent rowData,
        ImageRoiType defaultRoiType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        slideWorkingManager.AddWorkingPresentation(outputPath);
        var newSlide = slideWorkingManager.CopyFirstSlideToLast(outputPath);

        await ProcessTextReplacementsAsync(newSlide, rowData, textConfigs, cancellationToken);
        await ProcessImageReplacementsAsync(newSlide, rowData, imageConfigs, cancellationToken);
    }

    private static async Task ProcessTextReplacementsAsync(
        SlidePart slidePart,
        RowContent rowData,
        TextConfig[] textConfigs,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var replacements = new ReplaceInstructions();
        foreach (var config in textConfigs)
        foreach (var header in config.Columns)
            if (rowData.TryGetValue(header, out var value) && value != null)
                replacements[header] = value;

        await TextReplacer.ReplaceAsync(slidePart, replacements);
    }

    private async Task ProcessImageReplacementsAsync(
        SlidePart slidePart,
        RowContent rowData,
        ImageConfig[] imageConfigs,
        CancellationToken cancellationToken)
    {
        foreach (var config in imageConfigs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var imageUrl = GetImageUrlFromRowData(rowData, config.Columns);
            if (!UrlUtils.TryNormalizeHttpsUrl(imageUrl, out var imageUri) || imageUri is null)
            {
                Logger.LogWarning(
                    "Invalid or missing image URL for shape {ShapeId}, keeping placeholder (URL: {URL})",
                    config.ShapeId, imageUrl);
                continue;
            }

            try
            {
                await ProcessSingleImageReplacementAsync(
                    slidePart, config, imageUri.ToString(), cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Failed to process image for shape {ShapeId}, keeping placeholder",
                    config.ShapeId);
            }
        }
    }

    private async Task ProcessSingleImageReplacementAsync(
        SlidePart slidePart,
        ImageConfig config,
        string imageUrl,
        CancellationToken cancellationToken)
    {
        var imagePath = await DownloadImageAsync(imageUrl, cancellationToken);
        if (imagePath == null) return;

        try
        {
            var shape = Presentation.GetShapeById(slidePart, config.ShapeId);
            var picture = Presentation.GetPictureById(slidePart, config.ShapeId);

            if (shape != null)
                await ProcessImageAsync(slidePart, shape, imagePath, config.RoiType, config.CropType);
            else if (picture != null)
                await ProcessImageAsync(slidePart, picture, imagePath, config.RoiType, config.CropType);
            else
                Logger.LogWarning("Shape {ShapeId} not found in slide", config.ShapeId);
        }
        finally
        {
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    private async Task ProcessImageAsync(
        SlidePart slidePart,
        PresentationShape shape,
        string imagePath,
        ImageRoiType roiType,
        ImageCropType cropType)
    {
        var targetSize = ImageReplacer.GetShapeSize(shape);
        var bytes = await imageService.CropImageAsync(imagePath, targetSize, roiType, cropType);

        using var stream = new MemoryStream(bytes, false);
        ImageReplacer.ReplaceImage(slidePart, shape, stream);
    }

    private async Task ProcessImageAsync(
        SlidePart slidePart,
        DrawingPicture picture,
        string imagePath,
        ImageRoiType roiType,
        ImageCropType cropType)
    {
        var targetSize = ImageReplacer.GetPictureSize(picture);
        var bytes = await imageService.CropImageAsync(imagePath, targetSize, roiType, cropType);

        using var stream = new MemoryStream(bytes);
        ImageReplacer.ReplaceImage(slidePart, picture, stream);
    }

    private async Task<string?> DownloadImageAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var downloadTask = downloadService.CreateImageTask(url,
                new DirectoryInfo(ConfigHolder.Value.Download.SaveFolder));

            var tcs = new TaskCompletionSource<bool>();
            Exception? downloadException = null;

            downloadTask.DownloadCompletedEvents += (_, e) =>
            {
                if (e.Success)
                {
                    tcs.TrySetResult(true);
                }
                else
                {
                    downloadException = e.Error;
                    tcs.TrySetResult(false);
                }
            };

            await using var registration = cancellationToken.Register(() =>
            {
                downloadTask.Cancel();
                tcs.TrySetCanceled(cancellationToken);
            });

            await downloadService.DownloadTask(downloadTask);
            var success = await tcs.Task;

            if (!success)
            {
                Logger.LogWarning(downloadException, "Failed to download image from {Url}", url);
                if (File.Exists(downloadTask.FilePath))
                    File.Delete(downloadTask.FilePath);
                return null;
            }

            return downloadTask.FilePath;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error downloading image from {Url}", url);
            return null;
        }
    }

    private static string? GetImageUrlFromRowData(RowContent rowData, string[] columns)
    {
        foreach (var column in columns)
            if (rowData.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        return null;
    }
}