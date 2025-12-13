using System.Drawing;
using TaoSlideTotNghiep.Domain.Image.Enums;

namespace TaoSlideTotNghiep.Application.Image.Contracts;

/// <summary>
/// Interface for image processing service.
/// </summary>
public interface IImageService
{
    Rectangle CropImage(string filePath, ImageRoiType roiType, Size size);
}