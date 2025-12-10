using System.Drawing;
using Application.DTOs.Requests;

namespace Application.Contracts;

/// <summary>
/// Interface for image processing service.
/// </summary>
public interface IImageService
{
    Rectangle CropImage(string filePath, Size size, CropMode mode);
}