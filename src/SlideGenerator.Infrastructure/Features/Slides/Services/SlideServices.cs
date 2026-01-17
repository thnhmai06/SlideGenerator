using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Features.Configs;
using SlideGenerator.Application.Features.Images;
using SlideGenerator.Application.Features.Slides;
using SlideGenerator.Domain.Features.Downloads;
using SlideGenerator.Domain.Features.Jobs.Components;
using SlideGenerator.Framework.Cloud;
using SlideGenerator.Framework.Cloud.Exceptions;
using SlideGenerator.Framework.Slide;
using SlideGenerator.Infrastructure.Common.Base;
using SlideGenerator.Infrastructure.Common.Utilities;
using SlideGenerator.Infrastructure.Features.Images.Exceptions;
using Path = System.IO.Path;
using Presentation = SlideGenerator.Framework.Slide.Models.Presentation;

namespace SlideGenerator.Infrastructure.Features.Slides.Services;

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

        await checkpoint(JobCheckpointStage.BeforeSlideUpdate, cancellationToken);
        var (replacedCount, internalDetails) = await TextReplacer.ReplaceAsync(slidePart, replacements);
        await checkpoint(JobCheckpointStage.AfterSlideUpdate, cancellationToken);

        var details = internalDetails
            .Select(d => new TextReplacementDetail(d.ShapeId, d.Placeholder, d.Value))
            .ToList();

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
        var details = new List<ImageReplacementDetail>();
        var successCount = 0;

        var slideLock = new object();

        await Parallel.ForEachAsync(imageConfigs, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2)
        }, async (config, ct) =>
        {
            var imageSource = GetImageSourceFromRowData(rowData, config.Columns);
            if (string.IsNullOrWhiteSpace(imageSource))
                return;

            string? imagePath = null;
            var isTempDownload = false;

            try
            {
                imagePath = await ResolveImagePathAsync(imageSource, checkpoint, ct);
                if (imagePath == null)
                {
                    lock (errors)
                    {
                        errors.Add($"Failed to resolve image source for shape {config.ShapeId}");
                    }

                    return;
                }

                isTempDownload = IsTemporaryDownload(imageSource, imagePath);

                var picture = Presentation.GetPictureById(slidePart, config.ShapeId);
                var shape = Presentation.GetShapeById(slidePart, config.ShapeId);

                if (shape == null && picture == null)
                    return;

                var targetSize = picture != null
                    ? ImageReplacer.GetPictureSize(picture)
                    : ImageReplacer.GetShapeSize(shape!);

                var bytes = await imageService.CropImageAsync(imagePath, targetSize, config.RoiType, config.CropType);

                lock (slideLock)
                {
                    using var stream = new MemoryStream(bytes, false);
                    if (picture != null)
                        ImageReplacer.ReplaceImage(slidePart, picture, stream);
                    else if (shape != null)
                        ImageReplacer.ReplaceImage(slidePart, shape!, stream);

                    successCount++;
                    details.Add(new ImageReplacementDetail(config.ShapeId, imageSource));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (CannotExtractUrlException ex)
            {
                Logger.LogWarning(
                    "The provided URL for shape {ShapeId} cannot be resolved: {Message} ({Url})",
                    config.ShapeId, ex.Message, ex.OriginalUrl);
            }
            catch (NotImageFileUrl ex)
            {
                Logger.LogWarning(
                    "The provided URL for shape {ShapeId} is not an image file: {Message} ({Url})",
                    config.ShapeId, ex.Message, ex.Url);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Failed to process image for shape {ShapeId}, keeping placeholder",
                    config.ShapeId);
                lock (errors)
                {
                    errors.Add($"Shape {config.ShapeId}: {ex.Message}");
                }
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
        });

        // Checkpoints inside parallel loop are tricky. We call them once at the end or begin, 
        // or accept that they will be called concurrently (Checkpoints must be thread-safe).
        // Assuming JobCheckpoint delegate is thread-safe or we don't strictly need precise intermediate progress here for speed.

        return new ImageReplacementOutcome(successCount, errors.Count, errors, details);
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

    private static ReplaceInstructions BuildReplacementIndex(ReplaceInstructions replacements)
    {
        var index = new ReplaceInstructions(StringComparer.Ordinal);
        foreach (var (key, value) in replacements)
        {
            var normalized = NormalizePlaceholder(key);
            index.TryAdd(normalized, value);
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