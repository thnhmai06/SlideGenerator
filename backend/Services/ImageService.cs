using System.Drawing;
using TaoSlideTotNghiep.DTOs.Requests;
using TaoSlideTotNghiep.Exceptions;
using TaoSlideTotNghiep.Models;

namespace TaoSlideTotNghiep.Services;

/// <summary>
/// Interface for image processing service.
/// </summary>
public interface IImageService
{
    Rectangle CropImage(string filePath, Size size, CropMode mode);
}

/// <summary>
/// Image processing service implementation.
/// </summary>
public class ImageService(ILogger<ImageService> logger) : Service(logger), IImageService
{
    public Rectangle CropImage(string filePath, Size size, CropMode mode)
    {
        using var processor = new Image(filePath);

        var roi = mode switch
        {
            CropMode.Prominent => processor.GetProminentCrop(size).Roi,
            CropMode.Center => processor.GetCenterCrop(size),
            _ => throw new TypeNotIncludedException(typeof(CropMode))
        };

        processor.Crop(roi);
        processor.Save();

        Logger.LogInformation("Cropped image {FilePath} at ({X}, {Y}) with size {Width}x{Height} using mode {Mode}",
            filePath, roi.X, roi.Y, roi.Width, roi.Height, mode);

        return roi;
    }
}