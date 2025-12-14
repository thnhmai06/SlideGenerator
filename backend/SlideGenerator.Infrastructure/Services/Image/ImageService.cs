using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Configs;
using SlideGenerator.Application.Image.Contracts;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Framework.Image;
using SlideGenerator.Framework.Image.Configs;
using SlideGenerator.Framework.Image.Enums;
using SlideGenerator.Framework.Image.Models;
using SlideGenerator.Infrastructure.Services.Base;

namespace SlideGenerator.Infrastructure.Services.Image;

/// <summary>
///     Image processing service implementation.
/// </summary>
public class ImageService(ILogger<ImageService> logger) : Service(logger),
    IImageService
{
    private readonly SemaphoreSlim _processorLock = new(1, 1);
    private ImageProcessor? _imageProcessor;

    public Rectangle CropImage(string filePath, ImageRoiType roiType, Size size)
    {
        using var image = new ImageData(filePath);

        var coreRoiType = roiType switch
        {
            ImageRoiType.Attention => RoiType.Attention,
            ImageRoiType.Prominent => RoiType.Prominent,
            ImageRoiType.Center => RoiType.Center,
            _ => throw new ArgumentOutOfRangeException(nameof(roiType), roiType, null)
        };

        Rectangle roi;

        switch (coreRoiType)
        {
            case RoiType.Attention:
            {
                var processor = GetOrCreateImageProcessor();
                roi = processor.GetRoi(image, size, coreRoiType);
                break;
            }
            case RoiType.Prominent:
            {
                var processor = GetOrCreateImageProcessor();
                roi = processor.GetProminentRoi(image, size);
                break;
            }
            case RoiType.Center:
                roi = ImageProcessor.GetCenterRoi(image, size);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ImageProcessor.Crop(image, roi);
        image.Save();

        Logger.LogInformation("Cropped image {FilePath} at ({X}, {Y}) with size {Width}x{Height} using mode {Mode}",
            filePath, roi.X, roi.Y, roi.Width, roi.Height, roiType);

        return roi;
    }

    private ImageProcessor GetOrCreateImageProcessor()
    {
        if (_imageProcessor != null) return _imageProcessor;

        _processorLock.Wait();
        try
        {
            if (_imageProcessor != null) return _imageProcessor;

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

            _imageProcessor = new ImageProcessor(roiOptions);
            _imageProcessor.InitFaceModelAsync().Wait();

            return _imageProcessor;
        }
        finally
        {
            _processorLock.Release();
        }
    }
}