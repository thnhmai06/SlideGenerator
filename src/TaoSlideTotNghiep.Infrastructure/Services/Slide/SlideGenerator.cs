using System.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using TaoSlideTotNghiep.Application.Configs;
using TaoSlideTotNghiep.Application.Slide.Contracts;
using TaoSlideTotNghiep.Domain.Image.Enums;
using TaoSlideTotNghiep.Domain.Slide.Components;
using TaoSlideTotNghiep.Infrastructure.Engines.Image;
using TaoSlideTotNghiep.Infrastructure.Engines.Slide;
using TaoSlideTotNghiep.Infrastructure.Engines.Slide.Models;
using TaoSlideTotNghiep.Infrastructure.Services.Base;
using TaoSlideTotNghiep.Infrastructure.Utilities;
using DrawingPicture = DocumentFormat.OpenXml.Drawing.Picture;
using EngineImage = TaoSlideTotNghiep.Infrastructure.Engines.Image.Models.Image;
using Presentation = TaoSlideTotNghiep.Infrastructure.Engines.Slide.Models.Presentation;
using PresentationShape = DocumentFormat.OpenXml.Presentation.Shape;
using TextReplacementEngine = TaoSlideTotNghiep.Infrastructure.Engines.Slide.TextReplacementEngine;

namespace TaoSlideTotNghiep.Infrastructure.Services.Slide;

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

    private async Task ProcessTextReplacements(
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
            await TextReplacementEngine.ReplaceTextTemplate(slidePart, replacements);
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

                var shape = Presentation.GetShapeInSlide(slidePart, config.ShapeId);
                var picture = Presentation.GetPictureInSlide(slidePart, config.ShapeId);

                if (shape != null)
                {
                    var targetSize = GetShapeSize(shape);
                    await CropAndReplaceShapeImage(slidePart, shape, imagePath, targetSize, config.RoiType,
                        cancellationToken);
                }
                else if (picture != null)
                {
                    var targetSize = GetPictureSize(picture);
                    await CropAndReplacePictureImage(slidePart, picture, imagePath, targetSize, config.RoiType,
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

            var correctedUrl = await HttpUtils.CorrectImageUrlAsync(url, httpClient);
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

    private async Task CropAndReplaceShapeImage(
        SlidePart slidePart,
        PresentationShape shape,
        string imagePath,
        Size targetSize,
        ImageRoiType roiType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var image = new EngineImage(imagePath);
        var roi = ImageEngine.GetRoi(image, roiType, targetSize);
        ImageEngine.Crop(image, roi);

        var croppedPath = Path.ChangeExtension(imagePath, ".cropped.png");
        image.Save(croppedPath);

        await using var stream = File.OpenRead(croppedPath);
        ReplaceShapeImage(slidePart, shape, stream);

        if (File.Exists(croppedPath)) File.Delete(croppedPath);
    }

    private async Task CropAndReplacePictureImage(
        SlidePart slidePart,
        DrawingPicture picture,
        string imagePath,
        Size targetSize,
        ImageRoiType roiType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var image = new EngineImage(imagePath);
        var roi = ImageEngine.GetRoi(image, roiType, targetSize);
        ImageEngine.Crop(image, roi);

        var croppedPath = Path.ChangeExtension(imagePath, ".cropped.png");
        image.Save(croppedPath);

        await using var stream = File.OpenRead(croppedPath);
        ImageReplacementEngine.ReplaceImage(slidePart, picture, stream);

        if (File.Exists(croppedPath)) File.Delete(croppedPath);
    }

    private static void ReplaceShapeImage(SlidePart slidePart, PresentationShape shape, Stream pngStream)
    {
        var imgPart = slidePart.AddImagePart(ImagePartType.Png);
        imgPart.FeedData(pngStream);
        var rId = slidePart.GetIdOfPart(imgPart);

        var blipFill = shape.ShapeProperties?.GetFirstChild<DocumentFormat.OpenXml.Drawing.BlipFill>();
        var blip = blipFill?.Blip;
        if (blip?.Embed == null)
        {
            var fillProps = shape.ShapeProperties?.GetFirstChild<DocumentFormat.OpenXml.Drawing.FillProperties>();
            blipFill = fillProps?.GetFirstChild<DocumentFormat.OpenXml.Drawing.BlipFill>();
            blip = blipFill?.Blip;
        }

        if (blip?.Embed != null)
        {
            blip.Embed.Value = rId;
            slidePart.Slide.Save();
        }
    }

    private static Size GetShapeSize(PresentationShape shape)
    {
        var transform = shape.ShapeProperties?.Transform2D;
        if (transform?.Extents == null)
            return new Size(400, 300);

        var width = (int)((transform.Extents.Cx?.Value ?? 3810000) / 9525);
        var height = (int)((transform.Extents.Cy?.Value ?? 2857500) / 9525);
        return new Size(Math.Max(1, width), Math.Max(1, height));
    }

    private static Size GetPictureSize(DrawingPicture picture)
    {
        var transform = picture.ShapeProperties?.Transform2D;
        if (transform?.Extents == null)
            return new Size(400, 300);

        var width = (int)((transform.Extents.Cx?.Value ?? 3810000) / 9525);
        var height = (int)((transform.Extents.Cy?.Value ?? 2857500) / 9525);
        return new Size(Math.Max(1, width), Math.Max(1, height));
    }
}