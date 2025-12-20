using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Configs;
using SlideGenerator.Application.Image.Contracts;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Framework.Image;
using SlideGenerator.Framework.Image.Configs;
using SlideGenerator.Framework.Image.Enums;
using SlideGenerator.Framework.Image.Models;
using SlideGenerator.Infrastructure.Base;

namespace SlideGenerator.Infrastructure.Image.Services;

/// <summary>
///     Image processing service implementation.
/// </summary>
public class ImageService(ILogger<ImageService> logger) : Service(logger),
    IImageService
{
    private readonly Lazy<ImageProcessor> _imageProcessor = new(
        () =>
        {
            var imageConfig = ConfigHolder.Value.Image;
            var roiOptions = new RoiOptions
            {
                FaceConfidence = imageConfig.Face.Confidence,
                FacePaddingRatio = new ExpandRatio(
                    imageConfig.Face.PaddingTop,
                    imageConfig.Face.PaddingBottom,
                    imageConfig.Face.PaddingLeft,
                    imageConfig.Face.PaddingRight
                ),
                FacesUnionAll = imageConfig.Face.UnionAll,
                SaliencyPaddingRatio = new ExpandRatio(
                    imageConfig.Saliency.PaddingTop,
                    imageConfig.Saliency.PaddingBottom,
                    imageConfig.Saliency.PaddingLeft,
                    imageConfig.Saliency.PaddingRight
                )
            };

            var processor = new ImageProcessor(roiOptions);
            return processor;
        },
        LazyThreadSafetyMode.ExecutionAndPublication);

    public async Task<byte[]> CropImageAsync(string filePath, Size size, ImageRoiType roiType, ImageCropType cropType)
    {
        using var image = new ImageData(filePath);

        var coreRoiType = roiType switch
        {
            ImageRoiType.Attention => RoiType.Attention,
            ImageRoiType.Prominent => RoiType.Prominent,
            ImageRoiType.Center => RoiType.Center,
            _ => throw new ArgumentOutOfRangeException(nameof(roiType), roiType, null)
        };
        var coreCropType = cropType switch
        {
            ImageCropType.Crop => CropType.Crop,
            ImageCropType.Fit => CropType.Fit,
            _ => throw new ArgumentOutOfRangeException(nameof(cropType), cropType, null)
        };

        var roiSelector = _imageProcessor.Value.GetRoiSelector(coreRoiType);
        await ImageProcessor.CropToRoiAsync(image, size, roiSelector, coreCropType);
        Logger.LogInformation("Cropped image {FilePath} to size {Width}x{Height} (Roi: {RoiMode}, Crop: {CropMode})",
            filePath, image.Size.Width, image.Size.Height, roiType, cropType);

        return image.ToByteArray();
    }
}