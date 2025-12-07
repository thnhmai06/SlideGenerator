using TaoSlideTotNghiep.Logic;

namespace TaoSlideTotNghiep.Services;

/// <summary>
/// Interface for image processing service.
/// </summary>
public interface IImageService
{
    (int X, int Y) CropImage(string filePath, int width, int height, string mode);
}

/// <summary>
/// Image processing service implementation.
/// </summary>
public class ImageService(ILogger<ImageService> logger) : Service(logger), IImageService
{
    public (int X, int Y) CropImage(string filePath, int width, int height, string mode)
    {
        using var processor = new Image(filePath);

        int x, y;

        if (mode.Equals("prominent", StringComparison.OrdinalIgnoreCase))
        {
            var (topLeft, _) = processor.GetProminentCrop(width, height);
            x = topLeft.X;
            y = topLeft.Y;
        }
        else
        {
            var topLeft = processor.GetCenterCrop(width, height);
            x = topLeft.X;
            y = topLeft.Y;
        }

        processor.Crop(x, y, width, height);
        processor.Save();

        logger.LogInformation("Cropped image {FilePath} at ({X}, {Y}) with size {Width}x{Height}",
            filePath, x, y, width, height);

        return (x, y);
    }
}