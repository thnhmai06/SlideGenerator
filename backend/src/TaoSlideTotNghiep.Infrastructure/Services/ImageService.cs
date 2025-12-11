using Microsoft.Extensions.Logging;
using System.Drawing;
using TaoSlideTotNghiep.Application.Contracts;
using TaoSlideTotNghiep.Infrastructure.Engines;
using TaoSlideTotNghiep.Infrastructure.Engines.Models;

namespace TaoSlideTotNghiep.Infrastructure.Services;

/// <summary>
/// Image processing service implementation.
/// </summary>
public class ImageService(ILogger<ImageService> logger) : Service(logger),
    IImageService
{
    public Rectangle CropImage(string filePath, RoiType roiType, Size size)
    {
        using var image = new Image(filePath);

        var roi = ImageEngine.GetRoi(image, roiType, size);
        ImageEngine.Crop(image, roi);
        image.Save();

        Logger.LogInformation("Cropped image {FilePath} at ({X}, {Y}) with size {Width}x{Height} using mode {Mode}",
            filePath, roi.X, roi.Y, roi.Width, roi.Height, roiType);

        return roi;
    }
}