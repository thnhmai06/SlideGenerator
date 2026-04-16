using SlideGenerator.Application.Images.Services;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Rules;

namespace SlideGenerator.Infrastructure.Images.Services;

/// <summary>
///     Provides OpenCV-based face detector creation for <see cref="FaceDetectorManager" />.
/// </summary>
public sealed class OpenCvFaces : FaceDetectorManager
{
    public override ICollection<FaceDetectorModel> SupportedDetectors { get; } = [FaceDetectorModel.YuNet];

    /// <inheritdoc />
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
