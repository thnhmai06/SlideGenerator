using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.Extensions.Logging;
using SlideGenerator.Framework.Image.Modules.FaceDetection.Models;
using CoreImage = SlideGenerator.Framework.Image.Models.Image;

namespace SlideGenerator.Infrastructure.Features.Images.Services;

/// <summary>
///     A wrapper for <see cref="FaceDetectorModel" /> that resizes images before detection to improve performance.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="ResizingFaceDetectorModel" /> class.
/// </remarks>
/// <param name="inner">The inner face detector model.</param>
/// <param name="maxDimensionProvider">A function that returns the maximum allowed dimension (width or height).</param>
/// <param name="logger">The logger instance.</param>
public sealed class ResizingFaceDetectorModel(FaceDetectorModel inner, Func<int> maxDimensionProvider, ILogger logger)
    : FaceDetectorModel
{
    private readonly FaceDetectorModel _inner = inner;
    private readonly ILogger _logger = logger;
    private readonly Func<int> _maxDimensionProvider = maxDimensionProvider;

    public override bool IsModelAvailable => _inner.IsModelAvailable;

    public override void Dispose()
    {
        _inner.Dispose();
    }

    public override Task<bool> InitAsync()
    {
        return _inner.InitAsync();
    }

    public override Task<bool> DeInitAsync()
    {
        return _inner.DeInitAsync();
    }

    /// <summary>
    ///     Detects faces in the image, resizing it first if it exceeds the maximum dimension.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <param name="minScore">The minimum confidence score.</param>
    /// <returns>A list of detected faces with coordinates scaled back to the original image size.</returns>
    public override async Task<List<Face>> DetectAsync(CoreImage image, float minScore)
    {
        var maxDim = _maxDimensionProvider();

        // If maxDim is 0 or negative, resizing is disabled.
        var size = image.Size;
        if (maxDim <= 0 || (size.Width <= maxDim && size.Height <= maxDim))
            return await _inner.DetectAsync(image, minScore);

        // Calculate new size
        var scale = size.Width > size.Height
            ? (double)maxDim / size.Width
            : (double)maxDim / size.Height;

        var newWidth = (int)(size.Width * scale);
        var newHeight = (int)(size.Height * scale);
        var newSize = new Size(newWidth, newHeight);

        _logger.LogInformation(
            "Resizing image for face detection from {Width}x{Height} to {NewWidth}x{NewHeight} (Scale: {Scale:F4})",
            size.Width, size.Height, newWidth, newHeight, scale);

        CoreImage? resizedImage = null;
        try
        {
            // Create resized Mat
            var resizedMat = new Mat();
            CvInvoke.Resize(image.Mat, resizedMat, newSize, 0, 0, Inter.Area);

            // Create a dummy image instance without constructor
            resizedImage = (CoreImage)RuntimeHelpers.GetUninitializedObject(typeof(CoreImage));

            // Set properties via reflection
            // Mat
            var matProp = typeof(CoreImage).GetProperty("Mat", BindingFlags.Public | BindingFlags.Instance);
            if (matProp != null)
            {
                matProp.SetValue(resizedImage, resizedMat);
            }
            else
            {
                // Fallback to field if property not found (unlikely as it is public)
                resizedMat.Dispose();
                throw new InvalidOperationException("Could not find Mat property on Image class.");
            }

            // SourceName
            var sourceNameField = typeof(CoreImage).GetField("<SourceName>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            sourceNameField?.SetValue(resizedImage, $"{image.SourceName} (Resized)");

            var faces = await _inner.DetectAsync(resizedImage, minScore);

            // Scale faces back
            var scaledFaces = new List<Face>(faces.Count);
            foreach (var face in faces) scaledFaces.Add(ScaleFace(face, 1.0 / scale));
            return scaledFaces;
        }
        finally
        {
            resizedImage?.Dispose();
        }
    }

    private static Face ScaleFace(Face face, double scale)
    {
        var rect = new Rectangle(
            (int)Math.Round(face.Rect.X * scale),
            (int)Math.Round(face.Rect.Y * scale),
            (int)Math.Round(face.Rect.Width * scale),
            (int)Math.Round(face.Rect.Height * scale)
        );

        Point? ScalePoint(Point? p)
        {
            return p.HasValue
                ? new Point((int)Math.Round(p.Value.X * scale), (int)Math.Round(p.Value.Y * scale))
                : null;
        }

        return new Face(
            rect,
            face.Score,
            ScalePoint(face.RightEye),
            ScalePoint(face.LeftEye),
            ScalePoint(face.Nose),
            ScalePoint(face.RightMouth),
            ScalePoint(face.LeftMouth)
        );
    }
}