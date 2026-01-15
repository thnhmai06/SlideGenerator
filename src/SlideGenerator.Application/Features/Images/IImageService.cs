using System.Drawing;
using SlideGenerator.Domain.Features.Images.Enums;

namespace SlideGenerator.Application.Features.Images;

/// <summary>
///     Interface for image processing service.
/// </summary>
public interface IImageService
{
    /// <summary>
    ///     Gets a value indicating whether the face detection model is currently available and initialized.
    /// </summary>
    bool IsFaceModelAvailable { get; }

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

    /// <summary>
    ///     Initializes the face detection model asynchronously.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is <see langword="true" /> if the model
    ///     was successfully initialized; otherwise, <see langword="false" />.
    /// </returns>
    Task<bool> InitFaceModelAsync();

    /// <summary>
    ///     Deinitializes the face detection model asynchronously.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is <see langword="true" /> if the model
    ///     was successfully deinitialized; otherwise, <see langword="false" />.
    /// </returns>
    Task<bool> DeInitFaceModelAsync();
}