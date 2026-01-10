using System.Drawing;
using SlideGenerator.Domain.Features.Images.Enums;

namespace SlideGenerator.Application.Features.Images;

/// <summary>
///     Interface for image processing service.
/// </summary>
public interface IImageService
{
    /// <summary>
    ///     Crops the specified image file to the given size and region of interest using the specified crop type
    ///     asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the image file to be cropped. Cannot be null or empty.</param>
    /// <param name="size">The target size, in pixels, for the cropped image.</param>
    /// <param name="roiType">The region of interest type that determines which part of the image will be cropped.</param>
    /// <param name="cropType">The cropping method to apply to the image.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a byte array with the cropped image
    ///     data in the original file's format.
    /// </returns>
    Task<byte[]> CropImageAsync(string filePath, Size size, ImageRoiType roiType, ImageCropType cropType);
}