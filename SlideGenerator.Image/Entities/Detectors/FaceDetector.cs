using OpenCvSharp;
using SlideGenerator.Image.Models;

namespace SlideGenerator.Image.Entities.Detectors;

public abstract class FaceDetector : IDisposable
{
    public abstract void Dispose();

    /// <summary>
    ///     Detects faces in the specified image.
    /// </summary>
    public abstract Task<IReadOnlyList<Face>> DetectAsync(Mat mat);
}