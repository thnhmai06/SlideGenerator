using System.Drawing;

namespace TaoSlideTotNghiep.Application.Contracts;

public enum RoiType
{
    Prominent,
    Center
}

/// <summary>
/// Interface for image processing service.
/// </summary>
public interface IImageService
{
    Rectangle CropImage(string filePath, RoiType roiType, Size size);
}