using System.Drawing;
using SlideGenerator.Domain.Image.Enums;

namespace SlideGenerator.Application.Image.Contracts;

/// <summary>
/// Interface for image processing service.
/// </summary>
public interface IImageService
{
    Rectangle CropImage(string filePath, ImageRoiType roiType, Size size);
}