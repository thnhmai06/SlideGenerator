using OpenCvSharp;
using SlideGenerator.Images.Models;

namespace SlideGenerator.Images.Entities.Detectors;

public abstract class FaceDetector : IDisposable
{
    public abstract void Dispose();

    /// <summary>
    ///     Detects faces in the specified image.
    /// </summary>
    public abstract Task<IReadOnlyList<Face>> DetectAsync(Mat mat);
}