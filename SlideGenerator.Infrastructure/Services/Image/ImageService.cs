using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Image.Contracts;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Framework.Image;
using SlideGenerator.Framework.Image.Enums;
using SlideGenerator.Framework.Image.Models;
using SlideGenerator.Infrastructure.Services.Base;

namespace SlideGenerator.Infrastructure.Services.Image;

/// <summary>
/// Image processing service implementation.
/// </summary>
public class ImageService(ILogger<ImageService> logger) : Service(logger),
    IImageService
{
    public Rectangle CropImage(string filePath, ImageRoiType roiType, Size size)
    {
        using var image = new ImageData(filePath);

        var coreRoiType = roiType switch
        {
            ImageRoiType.Prominent => RoiType.Prominent,
            ImageRoiType.Center => RoiType.Center,
            _ => RoiType.Center
        };

        var roi = ImageProcessor.GetRoi(image, coreRoiType, size);
        ImageProcessor.Crop(image, roi);
        image.Save();

        Logger.LogInformation("Cropped image {FilePath} at ({X}, {Y}) with size {Width}x{Height} using mode {Mode}",
            filePath, roi.X, roi.Y, roi.Width, roi.Height, roiType);

        return roi;
    }
}