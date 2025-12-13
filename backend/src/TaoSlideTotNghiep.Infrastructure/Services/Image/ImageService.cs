using Microsoft.Extensions.Logging;
using System.Drawing;
using TaoSlideTotNghiep.Application.Image.Contracts;
using TaoSlideTotNghiep.Domain.Image.Enums;
using TaoSlideTotNghiep.Infrastructure.Engines.Image;
using TaoSlideTotNghiep.Infrastructure.Services.Base;

namespace TaoSlideTotNghiep.Infrastructure.Services.Image;

/// <summary>
/// Image processing service implementation.
/// </summary>
public class ImageService(ILogger<ImageService> logger) : Service(logger),
    IImageService
{
    public Rectangle CropImage(string filePath, ImageRoiType roiType, Size size)
    {
        using var image = new Engines.Image.Models.Image(filePath);

        var roi = ImageEngine.GetRoi(image, roiType, size);
        ImageEngine.Crop(image, roi);
        image.Save();

        Logger.LogInformation("Cropped image {FilePath} at ({X}, {Y}) with size {Width}x{Height} using mode {Mode}",
            filePath, roi.X, roi.Y, roi.Width, roi.Height, roiType);

        return roi;
    }
}