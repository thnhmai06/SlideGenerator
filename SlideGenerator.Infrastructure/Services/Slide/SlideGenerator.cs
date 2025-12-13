using System.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Configs;
using SlideGenerator.Application.Slide.Contracts;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Framework.Cloud;
using SlideGenerator.Framework.Image;
using SlideGenerator.Framework.Image.Enums;
using SlideGenerator.Framework.Image.Models;
using SlideGenerator.Framework.Slide;
using SlideGenerator.Framework.Slide.Models;
using SlideGenerator.Infrastructure.Services.Base;
using DrawingPicture = DocumentFormat.OpenXml.Drawing.Picture;
using PresentationShape = DocumentFormat.OpenXml.Presentation.Shape;
using Presentation = SlideGenerator.Framework.Slide.Models.Presentation;

namespace SlideGenerator.Infrastructure.Services.Slide;

public class SlideGenerator(
    ILogger<SlideGenerator> logger,
    IHttpClientFactory httpClientFactory) : Service(logger), ISlideGenerator
{
    public async Task ProcessRowAsync(
        string derivedPresentationPath,
        string templatePath,
        Dictionary<string, string?> rowData,
        TextConfig[] textConfigs,
        ImageConfig[] imageConfigs,
        ImageRoiType defaultRoiType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var derived = new DerivedPresentation(derivedPresentationPath, templatePath);
        var slideIdList = derived.GetSlideIdList();
        var firstSlideId = slideIdList.ChildElements.OfType<DocumentFormat.OpenXml.Presentation.SlideId>().First();
        var slideRId = firstSlideId.RelationshipId?.Value
                       ?? throw new InvalidOperationException("No slide relationship ID found");

        var newSlide = derived.CopySlide(slideRId, slideIdList.Count() + 1);

        await ProcessTextReplacements(newSlide, rowData, textConfigs, cancellationToken);
        await ProcessImageReplacements(newSlide, rowData, imageConfigs, cancellationToken);
    }

    private static async Task ProcessTextReplacements(
        SlidePart slidePart,
        Dictionary<string, string?> rowData,
        TextConfig[] textConfigs,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var replacements = new Dictionary<string, string>();

        foreach (var config in textConfigs)
        foreach (var column in config.Columns)
            if (rowData.TryGetValue(column, out var value) && value != null)
                replacements[column] = value;

        if (replacements.Count > 0)
            await TextReplacer.ReplaceAsync(slidePart, replacements);
    }

    private async Task ProcessImageReplacements(
        SlidePart slidePart,
        Dictionary<string, string?> rowData,
        ImageConfig[] imageConfigs,
        CancellationToken cancellationToken)
    {
        foreach (var config in imageConfigs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var imageUrl = GetImageUrlFromRowData(rowData, config.Columns);
            if (string.IsNullOrEmpty(imageUrl) || !IsValidUrl(imageUrl))
            {
                Logger.LogWarning("Invalid or missing image URL for shape {ShapeId}, keeping placeholder",
                    config.ShapeId);
                continue;
            }

            try
            {
                var imagePath = await DownloadImageAsync(imageUrl, cancellationToken);
                if (imagePath == null) continue;

                var shape = Presentation.GetShapeById(slidePart, config.ShapeId);
                var picture = Presentation.GetPictureById(slidePart, config.ShapeId);

                if (shape != null)
                {
                    var targetSize = ImageReplacer.GetShapeSize(shape);
                    CropAndReplaceShapeImage(slidePart, shape, imagePath, targetSize, config.RoiType,
                        cancellationToken);
                }
                else if (picture != null)
                {
                    var targetSize = ImageReplacer.GetPictureSize(picture);
                    CropAndReplacePictureImage(slidePart, picture, imagePath, targetSize, config.RoiType,
                        cancellationToken);
                }
                else
                {
                    Logger.LogWarning("Shape {ShapeId} not found in slide", config.ShapeId);
                }

                if (File.Exists(imagePath)) File.Delete(imagePath);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to process image for shape {ShapeId}, keeping placeholder",
                    config.ShapeId);
            }
        }
    }

    private static string? GetImageUrlFromRowData(Dictionary<string, string?> rowData, string[] columns)
    {
        foreach (var column in columns)
            if (rowData.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        return null;
    }

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private async Task<string?> DownloadImageAsync(string url, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();
        var tempPath = Path.Combine(ConfigHolder.Value.Download.SaveFolder, $"{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            var correctedUrl = await CloudUrlResolver.ResolveAsync(url, httpClient);
            var response = await httpClient.GetAsync(correctedUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("Failed to download image from {Url}: {StatusCode}", url, response.StatusCode);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            var extension = GetExtensionFromContentType(contentType) ?? ".png";
            var finalPath = Path.ChangeExtension(tempPath, extension);

            await using var fileStream = File.Create(finalPath);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            return finalPath;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error downloading image from {Url}", url);
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return null;
        }
    }

    private static string? GetExtensionFromContentType(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            _ => null
        };
    }

    private static RoiType MapRoiType(ImageRoiType roiType)
    {
        return roiType switch
        {
            ImageRoiType.Prominent => RoiType.Prominent,
            ImageRoiType.Center => RoiType.Center,
            _ => RoiType.Center
        };
    }

    private static void CropAndReplaceShapeImage(
        SlidePart slidePart,
        PresentationShape shape,
        string imagePath,
        Size targetSize,
        ImageRoiType roiType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var image = new ImageData(imagePath);
        var roi = ImageProcessor.GetRoi(image, MapRoiType(roiType), targetSize);
        ImageProcessor.Crop(image, roi);

        var croppedPath = Path.ChangeExtension(imagePath, ".cropped.png");
        image.Save(croppedPath);

        using var stream = File.OpenRead(croppedPath);
        ImageReplacer.ReplaceImage(slidePart, shape, stream);

        if (File.Exists(croppedPath)) File.Delete(croppedPath);
    }

    private static void CropAndReplacePictureImage(
        SlidePart slidePart,
        DrawingPicture picture,
        string imagePath,
        Size targetSize,
        ImageRoiType roiType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var image = new ImageData(imagePath);
        var roi = ImageProcessor.GetRoi(image, MapRoiType(roiType), targetSize);
        ImageProcessor.Crop(image, roi);

        var croppedPath = Path.ChangeExtension(imagePath, ".cropped.png");
        image.Save(croppedPath);

        using var stream = File.OpenRead(croppedPath);
        ImageReplacer.ReplaceImage(slidePart, picture, stream);

        if (File.Exists(croppedPath)) File.Delete(croppedPath);
    }
}