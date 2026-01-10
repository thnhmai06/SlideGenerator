using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Features.Configs;
using SlideGenerator.Application.Features.Images;
using SlideGenerator.Domain.Features.Images.Enums;
using SlideGenerator.Framework.Image;
using SlideGenerator.Framework.Image.Configs;
using SlideGenerator.Framework.Image.Enums;
using SlideGenerator.Framework.Image.Exceptions;
using SlideGenerator.Framework.Image.Models;
using SlideGenerator.Infrastructure.Common.Base;

namespace SlideGenerator.Infrastructure.Features.Images.Services;

/// <summary>
///     Image processing service implementation.
/// </summary>
public class ImageService(ILogger<ImageService> logger) : Service(logger),
    IImageService, IDisposable
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

    public void Dispose()
    {
        if (_imageProcessor.IsValueCreated)
            _imageProcessor.Value.Dispose();
    }

    public async Task<byte[]> CropImageAsync(string filePath, Size size, ImageRoiType roiType, ImageCropType cropType)
    {
        using var image = new ImageData(filePath);
        try
        {
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
            Logger.LogInformation(
                "Cropped image {FilePath} to size {Width}x{Height} (Roi: {RoiMode}, Crop: {CropMode})",
                filePath, image.Size.Width, image.Size.Height, roiType, cropType);

            return image.ToByteArray();
        }
        catch (ReadImageFailed ex)
        {
            Logger.LogWarning(ex,
                "Image processing unavailable for {FilePath}. Using PNG bytes without ROI.",
                filePath);
            return image.ToByteArray();
        }
    }
}