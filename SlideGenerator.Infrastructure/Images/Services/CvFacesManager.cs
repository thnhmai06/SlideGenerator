using SlideGenerator.Application.Modules.Images.Services;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Rules;

namespace SlideGenerator.Infrastructure.Images.Services;

/// <summary>
///     Provides OpenCV-based face detector creation for <see cref="FaceDetectorsManager" />.
/// </summary>
public sealed class CvFacesManager : FaceDetectorsManager
{
    /// <inheritdoc />
    /// <summary>
    ///     Gets the collection of face detector models supported by this manager.
    /// </summary>
    public override ICollection<FaceDetectorModel> SupportedDetectors { get; } = [FaceDetectorModel.YuNet];

    /// <inheritdoc />
    /// <summary>
    ///     Creates a new instance of a <see cref="FaceDetector" /> for the specified model.
    /// </summary>
    /// <param name="model">The <see cref="FaceDetectorModel" /> to create.</param>
    /// <returns>A new <see cref="FaceDetector" /> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown when the specified model is not supported.</exception>
    protected override FaceDetector CreateDetector(FaceDetectorModel model)
    {
        switch (model)
        {
            case FaceDetectorModel.YuNet:
            // TODO: Define factory.
            // return new YuNet();
            default:
                throw new NotSupportedException($"The face detector model '{model}' is not supported.");
        }
    }
}