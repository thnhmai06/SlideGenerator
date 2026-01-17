using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Features.Configs;
using SlideGenerator.Application.Features.Images;
using SlideGenerator.Domain.Features.Images.Enums;
using SlideGenerator.Framework.Image.Exceptions;
using SlideGenerator.Framework.Image.Modules.FaceDetection.Models;
using SlideGenerator.Framework.Image.Modules.Roi;
using SlideGenerator.Framework.Image.Modules.Roi.Configs;
using SlideGenerator.Framework.Image.Modules.Roi.Enums;
using SlideGenerator.Framework.Image.Modules.Roi.Models;
using SlideGenerator.Infrastructure.Common.Base;
using Image = SlideGenerator.Framework.Image.Models.Image;

namespace SlideGenerator.Infrastructure.Features.Images.Services;

/// <summary>
///     Image processing service implementation.
/// </summary>
public sealed class ImageService : Service,
    IImageService, IDisposable
{
    private readonly FaceDetectorModel _faceDetectorMode;
    private readonly Lazy<RoiModule> _roiModule;

    public ImageService(ILogger<ImageService> logger) : base(logger)
    {
        var baseModel = new YuNetModel();
        _faceDetectorMode = new ResizingFaceDetectorModel(baseModel,
            () => ConfigHolder.Value.Image.Face.MaxDimension,
            logger);
        _roiModule = new Lazy<RoiModule>(
            () =>
            {
                var imageConfig = ConfigHolder.Value.Image;
                var roiOptions = new RoiOptions
                {
                    FaceConfidence = imageConfig.Face.Confidence,
                    FacesUnionAll = imageConfig.Face.UnionAll,
                    SaliencyPaddingRatio = new ExpandRatio(
                        imageConfig.Saliency.PaddingTop,
                        imageConfig.Saliency.PaddingBottom,
                        imageConfig.Saliency.PaddingLeft,
                        imageConfig.Saliency.PaddingRight
                    )
                };

                return new RoiModule(roiOptions)
                {
                    FaceDetectorModel = _faceDetectorMode
                };
            },
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public void Dispose()
    {
        _faceDetectorMode.Dispose();
    }

    /// <inheritdoc />
    public bool IsFaceModelAvailable => _faceDetectorMode.IsModelAvailable;

    /// <inheritdoc />
    public Task<bool> InitFaceModelAsync()
    {
        return _faceDetectorMode.InitAsync();
    }

    /// <inheritdoc />
    public Task<bool> DeInitFaceModelAsync()
    {
        return _faceDetectorMode.DeInitAsync();
    }

    public async Task<byte[]> CropImageAsync(string filePath, Size size, ImageRoiType roiType, ImageCropType cropType)
    {
        using var image = new Image(filePath);
        try
        {
            var coreRoiType = roiType switch
            {
                ImageRoiType.RuleOfThirds => RoiType.RuleOfThirds,
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

            var roiSelector = _roiModule.Value.GetRoiSelector(coreRoiType);
            await RoiModule.CropToRoiAsync(image, size, roiSelector, coreCropType);
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