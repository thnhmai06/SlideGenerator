using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Configs;
using SlideGenerator.Application.Image;
using SlideGenerator.Application.Slide;
using SlideGenerator.Domain.Download;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Domain.Job.Components;
using SlideGenerator.Framework.Cloud;
using SlideGenerator.Framework.Cloud.Exceptions;
using SlideGenerator.Framework.Slide;
using SlideGenerator.Infrastructure.Base;
using SlideGenerator.Infrastructure.Image.Exceptions;
using SlideGenerator.Infrastructure.Utilities;
using DrawingPicture = DocumentFormat.OpenXml.Drawing.Picture;
using Path = System.IO.Path;
using Presentation = SlideGenerator.Framework.Slide.Models.Presentation;
using PresentationShape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slide.Services;

using ReplaceInstructions = Dictionary<string, string>;
using RowContent = Dictionary<string, string?>;

public class SlideServices(
    ILogger<SlideServices> logger,
    IDownloadClient downloadClient,
    IImageService imageService,
    SlideWorkingManager slideWorkingManager,
    IHttpClientFactory httpClientFactory) : Service(logger), ISlideServices
{
    public async Task<RowProcessResult> ProcessRowAsync(
        string presentationPath,
        JobTextConfig[] textConfigs,
        JobImageConfig[] imageConfigs,
        RowContent rowData,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        slideWorkingManager.GetOrAddWorkingPresentation(presentationPath);
        var newSlide = slideWorkingManager.CopyFirstSlideToLast(presentationPath);

        var textResult =
            await ProcessTextReplacementsAsync(newSlide, rowData, textConfigs, checkpoint, cancellationToken);
        var imageResult = await ProcessImageReplacementsAsync(
            newSlide,
            rowData,
            imageConfigs,
            checkpoint,
            cancellationToken);
        return new RowProcessResult(
            textResult.Count,
            imageResult.Count,
            imageResult.ErrorCount,
            imageResult.Errors,
            textResult.Details,
            imageResult.Details);
    }

    public void RemoveFirstSlide(string presentationPath)
    {
        presentationPath = Path.GetFullPath(presentationPath);
        var presentation = slideWorkingManager.GetWorkingPresentation(presentationPath);

        if (presentation.SlideCount <= 1)
        {
            Logger.LogWarning("Skip removing first slide for {FilePath} because slide count is {SlideCount}",
                presentationPath, presentation.SlideCount);
            return;
        }

        presentation.RemoveSlide(1);
        presentation.Save();
        Logger.LogInformation("Removed template slide from {FilePath}", presentationPath);
    }

    private static async Task<TextReplacementOutcome> ProcessTextReplacementsAsync(
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
            return new TextReplacementOutcome(0, []);

        var details = CollectTextReplacementDetails(slidePart, replacements);
        await checkpoint(JobCheckpointStage.BeforeSlideUpdate, cancellationToken);
        var replacedCount = await TextReplacer.ReplaceAsync(slidePart, replacements);
        await checkpoint(JobCheckpointStage.AfterSlideUpdate, cancellationToken);
        return new TextReplacementOutcome((int)replacedCount, details);
    }

    private async Task<ImageReplacementOutcome> ProcessImageReplacementsAsync(
        SlidePart slidePart,
        RowContent rowData,
        JobImageConfig[] imageConfigs,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var imageReplacementCount = 0;
        var details = new List<ImageReplacementDetail>();

        foreach (var config in imageConfigs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var imageSource = GetImageSourceFromRowData(rowData, config.Columns);
            if (string.IsNullOrWhiteSpace(imageSource))
                continue;

            string? imagePath = null;
            var isTempDownload = false;

            try
            {
                imagePath = await ResolveImagePathAsync(imageSource, checkpoint, cancellationToken);
                if (imagePath == null)
                {
                    errors.Add($"Failed to resolve image source for shape {config.ShapeId}");
                    continue;
                }

                isTempDownload = IsTemporaryDownload(imageSource, imagePath);

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
                imageReplacementCount++;
                details.Add(new ImageReplacementDetail(config.ShapeId, imageSource));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (CannotExtractUrlException ex)
            {
                Logger.LogWarning(ex,
                    "The provided URL for shape {ShapeId} cannot be extracted, keeping placeholder ({Url})",
                    config.ShapeId, ex.OriginalUrl);
            }
            catch (NotImageFileUrl ex)
            {
                Logger.LogWarning(ex,
                    "The provided URL for shape {ShapeId} cannot be extracted, keeping placeholder ({Url})",
                    config.ShapeId, ex.Url);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Failed to process image for shape {ShapeId}, keeping placeholder",
                    config.ShapeId);
                errors.Add($"Shape {config.ShapeId}: {ex.Message}");
            }
            finally
            {
                if (isTempDownload && !string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
                    try
                    {
                        File.Delete(imagePath);
                    }
                    catch (IOException)
                    {
                        // Ignore cleanup failures for temp downloads.
                    }
            }
        }

        return new ImageReplacementOutcome(imageReplacementCount, errors.Count, errors, details);
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

    private static List<TextReplacementDetail> CollectTextReplacementDetails(
        SlidePart slidePart,
        ReplaceInstructions replacements)
    {
        var details = new List<TextReplacementDetail>();
        var replacementIndex = BuildReplacementIndex(replacements);

        foreach (var shape in Presentation.GetShapes(slidePart))
        {
            var shapeId = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value;
            if (shapeId is null) continue;

            var textBody = shape.TextBody;
            if (textBody is null) continue;

            var textRuns = textBody.Descendants<Text>().ToList();
            if (textRuns.Count == 0) continue;

            var original = string.Concat(textRuns.Select(run => run.Text));
            if (string.IsNullOrEmpty(original) || !original.Contains("{{", StringComparison.Ordinal))
                continue;

            var placeholders = TextReplacer.ScanPlaceholders(original);
            foreach (var placeholder in placeholders)
                if (replacementIndex.TryGetValue(placeholder, out var value))
                    details.Add(new TextReplacementDetail(shapeId.Value, placeholder, value));
        }

        return details;
    }

    private static Dictionary<string, string> BuildReplacementIndex(ReplaceInstructions replacements)
    {
        var index = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in replacements)
        {
            var normalized = NormalizePlaceholder(key);
            if (!index.ContainsKey(normalized))
                index[normalized] = value;
        }

        return index;
    }

    private static string NormalizePlaceholder(string key)
    {
        var trimmed = key.Trim();
        if (trimmed.StartsWith("{{", StringComparison.Ordinal)
            && trimmed.EndsWith("}}", StringComparison.Ordinal)
            && trimmed.Length > 4)
            return trimmed[2..^2].Trim();
        return trimmed;
    }

    private sealed record TextReplacementOutcome(int Count, List<TextReplacementDetail> Details);

    private sealed record ImageReplacementOutcome(
        int Count,
        int ErrorCount,
        List<string> Errors,
        List<ImageReplacementDetail> Details);
}