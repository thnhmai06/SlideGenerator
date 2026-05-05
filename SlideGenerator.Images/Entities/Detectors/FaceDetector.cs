using OpenCvSharp;
using SlideGenerator.Images.Models;

namespace SlideGenerator.Images.Entities.Detectors;

public abstract class FaceDetector : IDisposable
{
    /// <summary>
    ///     Detects faces in the specified image.
    /// </summary>
    public abstract Task<IReadOnlyList<Face>> DetectAsync(Mat mat);

    public abstract void Dispose();
}
