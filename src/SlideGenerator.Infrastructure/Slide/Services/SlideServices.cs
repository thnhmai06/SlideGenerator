using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Configs;
using SlideGenerator.Application.Image;
using SlideGenerator.Application.Slide;
using SlideGenerator.Domain.Download;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Domain.Job.Components;
using SlideGenerator.Framework.Cloud;
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
    IDownloadClient downloadClient,
    IImageService imageService,
    SlideWorkingManager slideWorkingManager,
    IHttpClientFactory httpClientFactory) : Service(logger), ISlideServices
{
    public async Task<RowProcessResult> ProcessRowAsync(
        string outputPath,
        string templatePath,
        JobTextConfig[] textConfigs,
        JobImageConfig[] imageConfigs,
        RowContent rowData,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        slideWorkingManager.AddWorkingPresentation(outputPath);
        var newSlide = slideWorkingManager.CopyFirstSlideToLast(outputPath);

        await ProcessTextReplacementsAsync(newSlide, rowData, textConfigs, checkpoint, cancellationToken);
        return await ProcessImageReplacementsAsync(
            newSlide,
            rowData,
            imageConfigs,
            checkpoint,
            cancellationToken);
    }

    private static async Task ProcessTextReplacementsAsync(
        SlidePart slidePart,
        RowContent rowData,
        JobTextConfig[] textConfigs,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var replacements = new ReplaceInstructions();
        foreach (var config in textConfigs)
        foreach (var header in config.Columns)
        {
            if (!rowData.TryGetValue(header, out var value) || string.IsNullOrWhiteSpace(value)) continue;
            replacements[config.Pattern] = value;
            break;
        }

        if (replacements.Count == 0)
            return;

        await checkpoint(JobCheckpointStage.BeforeSlideUpdate, cancellationToken);
        await TextReplacer.ReplaceAsync(slidePart, replacements);
        await checkpoint(JobCheckpointStage.AfterSlideUpdate, cancellationToken);
    }

    private async Task<RowProcessResult> ProcessImageReplacementsAsync(
        SlidePart slidePart,
        RowContent rowData,
        JobImageConfig[] imageConfigs,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        foreach (var config in imageConfigs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var imageSource = GetImageSourceFromRowData(rowData, config.Columns);
            if (string.IsNullOrWhiteSpace(imageSource))
                continue;

            try
            {
                var imagePath = await ResolveImagePathAsync(imageSource, checkpoint, cancellationToken);
                if (imagePath == null)
                {
                    errors.Add($"Failed to resolve image source for shape {config.ShapeId}");
                    continue;
                }

                var roiType = config.RoiType;
                var cropType = config.CropType;

                await ProcessSingleImageReplacementAsync(
                    slidePart,
                    config.ShapeId,
                    imagePath,
                    roiType,
                    cropType,
                    checkpoint,
                    cancellationToken);

                if (IsTemporaryDownload(imageSource, imagePath) && File.Exists(imagePath))
                    File.Delete(imagePath);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Failed to process image for shape {ShapeId}, keeping placeholder",
                    config.ShapeId);
                errors.Add($"Shape {config.ShapeId}: {ex.Message}");
            }
        }

        return new RowProcessResult(errors.Count, errors);
    }

    private async Task ProcessSingleImageReplacementAsync(
        SlidePart slidePart,
        uint shapeId,
        string imagePath,
        ImageRoiType roiType,
        ImageCropType cropType,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        var shape = Presentation.GetShapeById(slidePart, shapeId);
        var picture = Presentation.GetPictureById(slidePart, shapeId);

        if (shape == null && picture == null)
            throw new InvalidOperationException($"Shape {shapeId} not found in slide.");

        await checkpoint(JobCheckpointStage.BeforeImageProcess, cancellationToken);
        if (shape != null)
            await ProcessImageAsync(slidePart, shape, imagePath, roiType, cropType, checkpoint, cancellationToken);
        else if (picture != null)
            await ProcessImageAsync(slidePart, picture, imagePath, roiType, cropType, checkpoint, cancellationToken);
        await checkpoint(JobCheckpointStage.AfterImageProcess, cancellationToken);
    }

    private async Task ProcessImageAsync(
        SlidePart slidePart,
        PresentationShape shape,
        string imagePath,
        ImageRoiType roiType,
        ImageCropType cropType,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        var targetSize = ImageReplacer.GetShapeSize(shape);
        var bytes = await imageService.CropImageAsync(imagePath, targetSize, roiType, cropType);

        await checkpoint(JobCheckpointStage.BeforeSlideUpdate, cancellationToken);
        using var stream = new MemoryStream(bytes, false);
        ImageReplacer.ReplaceImage(slidePart, shape, stream);
        await checkpoint(JobCheckpointStage.AfterSlideUpdate, cancellationToken);
    }

    private async Task ProcessImageAsync(
        SlidePart slidePart,
        DrawingPicture picture,
        string imagePath,
        ImageRoiType roiType,
        ImageCropType cropType,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        var targetSize = ImageReplacer.GetPictureSize(picture);
        var bytes = await imageService.CropImageAsync(imagePath, targetSize, roiType, cropType);

        await checkpoint(JobCheckpointStage.BeforeSlideUpdate, cancellationToken);
        using var stream = new MemoryStream(bytes);
        ImageReplacer.ReplaceImage(slidePart, picture, stream);
        await checkpoint(JobCheckpointStage.AfterSlideUpdate, cancellationToken);
    }

    private async Task<string?> ResolveImagePathAsync(
        string imageSource,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        if (File.Exists(imageSource))
            return imageSource;

        if (!UrlUtils.TryNormalizeHttpsUrl(imageSource, out var imageUri) || imageUri is null)
            return null;

        await checkpoint(JobCheckpointStage.BeforeCloudResolve, cancellationToken);
        var resolvedUri = imageUri;
        if (CloudUrlResolver.IsCloudUrlSupported(imageUri))
        {
            var client = httpClientFactory.CreateClient();
            resolvedUri = await CloudUrlResolver.ResolveLinkAsync(imageUri, client);
        }

        await checkpoint(JobCheckpointStage.AfterCloudResolve, cancellationToken);

        await checkpoint(JobCheckpointStage.BeforeDownload, cancellationToken);
        var result = await downloadClient.DownloadAsync(resolvedUri,
            new DirectoryInfo(ConfigHolder.Value.Download.SaveFolder), cancellationToken);
        await checkpoint(JobCheckpointStage.AfterDownload, cancellationToken);

        return result.Success ? result.FilePath : null;
    }

    private static bool IsTemporaryDownload(string imageSource, string imagePath)
    {
        return !string.Equals(imageSource, imagePath, StringComparison.OrdinalIgnoreCase)
               && !File.Exists(imageSource);
    }

    private static string? GetImageSourceFromRowData(RowContent rowData, string[] columns)
    {
        foreach (var column in columns)
            if (rowData.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        return null;
    }
}