using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Images.Models;
using Point = System.Drawing.Point;
using Size = OpenCvSharp.Size;

namespace SlideGenerator.Infrastructure.Images.Adapters;

/// <summary>
///     Asynchronous wrapper for <see cref="FaceDetectorYN" />.
/// </summary>
/// <param name="modelPath">The path to the requested model.</param>
/// <param name="inputSize">The size of the input image that the model will resize to in detection.</param>
/// <param name="configPath">The path to the configuration file for compatibility; not required for ONNX models.</param>
/// <param name="scoreThreshold">The threshold to filter out bounding boxes with a score smaller than the given value.</param>
/// <param name="nmsThreshold">The threshold to suppress bounding boxes with an IoU larger than the given value.</param>
/// <param name="topK">The maximum number of bounding boxes to keep before Non-Maximum Suppression (NMS).</param>
/// <param name="backendId">The identifier of the backend to be used.</param>
/// <param name="targetId">The identifier of the target device to be used.</param>
public sealed class YuNet(
    string modelPath,
    Size inputSize,
    string? configPath = null,
    float scoreThreshold = 0.9f,
    float nmsThreshold = 0.3f,
    int topK = 5000,
    Backend backendId = Backend.DEFAULT,
    Target targetId = Target.CPU) : FaceDetector
{
    /// <summary>
    ///     The underlying OpenCV <see cref="FaceDetectorYN" /> instance.
    /// </summary>
    private FaceDetectorYN? _model;

    /// <summary>
    ///     Gets the semaphore used to coordinate access to lock detection operations.
    /// </summary>
    /// <remarks>
    ///     Use this semaphore to ensure that face detection logic is executed in a thread-safe manner.
    ///     The semaphore is initialized with a single slot, allowing only one concurrent operation. This property is
    ///     immutable and set during object initialization.
    /// </remarks>
    public SemaphoreSlim DetectLock { private get; init; } = new(1, 1);

    /// <inheritdoc />
    /// <summary>
    ///     Gets a value indicating whether the face detection model is currently available and not disposed.
    /// </summary>
    public override bool IsModelAvailable => _model is { IsDisposed: false };

    /// <inheritdoc />
    /// <summary>
    ///     Asynchronously disposes the model and releases associated resources.
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        await DeInitAsync().ConfigureAwait(false);
        DetectLock.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    /// <summary>
    ///     Initializes the <see cref="FaceDetectorYN" /> model instance.
    /// </summary>
    /// <returns>
    ///     A task representing the initialization operation, yielding <see langword="true" /> if initialization was
    ///     successful; otherwise, <see langword="false" />.
    /// </returns>
    public override Task<bool> InitAsync()
    {
        if (!IsModelAvailable)
            _model = FaceDetectorYN.Create(
                modelPath, configPath ?? string.Empty, inputSize,
                scoreThreshold, nmsThreshold, topK, backendId, targetId);

        return Task.FromResult(IsModelAvailable);
    }

    /// <inheritdoc />
    /// <summary>
    ///     De-initializes the <see cref="FaceDetectorYN" /> model and releases its resources.
    /// </summary>
    /// <returns>
    ///     A task representing the de-initialization operation, yielding <see langword="true" /> if the model was
    ///     successfully de-initialized; otherwise, <see langword="false" />.
    /// </returns>
    public override async Task<bool> DeInitAsync()
    {
        await DetectLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (IsModelAvailable)
            {
                _model?.Dispose();
                _model = null;
            }
        }
        finally
        {
            DetectLock.Release();
        }

        return !IsModelAvailable;
    }

    /// <inheritdoc />
    /// <summary>
    ///     Detects faces in the provided <see cref="IImage" /> instance.
    /// </summary>
    /// <param name="image">The image to process. Must be an instance of <see cref="Mat" />.</param>
    /// <returns>A task yielding a list of detected <see cref="Face" /> instances.</returns>
    public override async Task<IReadOnlyList<Face>> DetectAsync(IImage image)
    {
        return await DetectAsync(((Mat)image).Core).ConfigureAwait(false);
    }

    /// <summary>
    ///     Detects faces from the provided <see cref="OpenCvSharp.Mat" /> using the initialized model.
    /// </summary>
    /// <param name="mat">The input image matrix.</param>
    /// <returns>A task yielding a list of detected <see cref="Face" /> instances.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the model is not initialized.</exception>
    public async Task<List<Face>> DetectAsync(OpenCvSharp.Mat mat)
    {
        if (!IsModelAvailable)
            throw new InvalidOperationException("The model is not initialized.");

        var faces = new List<Face>();
        if (mat.Empty()) return faces;

        // Resize and pad image to match InputSize
        var resizeAndPadInfo = ResizeAndPadMat(mat);
        using var processedMat = resizeAndPadInfo.ProcessedMat;

        using var result = new OpenCvSharp.Mat();
        await DetectLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _model!.Detect(processedMat, result);
        }
        finally
        {
            DetectLock.Release();
        }

        if (result.Empty() || result.Rows == 0 || result.Cols < 15)
            return faces;

        var faceCount = result.Rows;
        var matBorder = new Rectangle(0, 0, mat.Width, mat.Height);

        for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
        {
            var score = result.At<float>(faceIndex, 14);
            var x = RoundToIntAwayFromZero(result.At<float>(faceIndex, 0));
            var y = RoundToIntAwayFromZero(result.At<float>(faceIndex, 1));
            var w = RoundToIntAwayFromZero(result.At<float>(faceIndex, 2));
            var h = RoundToIntAwayFromZero(result.At<float>(faceIndex, 3));

            // Unmap bounding box back to original image coordinates
            var mappedRect = UnmapBoundingBox(new Rectangle(x, y, w, h), resizeAndPadInfo);
            var rect = Rectangle.Intersect(mappedRect, matBorder);
            if (rect.Width <= 0 || rect.Height <= 0) continue;

            // Unmap landmarks back to original image coordinates
            var eyeRight = UnmapLandmark(new Point(
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 4)),
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 5))), resizeAndPadInfo);
            var eyeLeft = UnmapLandmark(new Point(
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 6)),
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 7))), resizeAndPadInfo);
            var nose = UnmapLandmark(new Point(
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 8)),
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 9))), resizeAndPadInfo);
            var mouthRight = UnmapLandmark(new Point(
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 10)),
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 11))), resizeAndPadInfo);
            var mouthLeft = UnmapLandmark(new Point(
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 12)),
                RoundToIntAwayFromZero(result.At<float>(faceIndex, 13))), resizeAndPadInfo);

            faces.Add(new Face(rect, score, eyeRight, eyeLeft, nose, mouthRight, mouthLeft));
        }

        return faces;
    }

    /// <summary>
    ///     Resizes and pads the input image to match <c>inputSize</c> with black padding.
    /// </summary>
    /// <remarks>
    ///     If input is smaller than <c>inputSize</c>, only adds black padding.
    ///     If input is larger, resizes proportionally while maintaining aspect ratio, then adds black padding.
    /// </remarks>
    /// <param name="mat">Original input image.</param>
    /// <returns>A <see cref="ResizeAndPadInfo" /> containing the processed image and parameters for unmapping coordinates.</returns>
    private ResizeAndPadInfo ResizeAndPadMat(OpenCvSharp.Mat mat)
    {
        var originalWidth = mat.Width;
        var originalHeight = mat.Height;
        var targetWidth = inputSize.Width;
        var targetHeight = inputSize.Height;

        // Calculate scale and new dimensions
        var scale = MathF.Min((float)targetWidth / originalWidth, (float)targetHeight / originalHeight);
        var newWidth = RoundToIntAwayFromZero(originalWidth * scale);
        var newHeight = RoundToIntAwayFromZero(originalHeight * scale);

        // Resize if necessary
        var resizedMat = mat.Clone();
        if (scale < 1.0f)
        {
            var cvSize = new Size(newWidth, newHeight);
            Utilities.Resize(ref resizedMat, cvSize, InterpolationFlags.Linear);
        }

        // Calculate padding offsets
        var padLeft = (targetWidth - newWidth) / 2;
        var padTop = (targetHeight - newHeight) / 2;

        // Create output image with black background
        using var processedMat =
            new OpenCvSharp.Mat(new Size(targetWidth, targetHeight), mat.Type(), new Scalar(0, 0, 0));
        var roi = new Rect(padLeft, padTop, newWidth, newHeight);
        resizedMat.CopyTo(processedMat[roi]);
        resizedMat.Dispose();

        return new ResizeAndPadInfo
        {
            ProcessedMat = processedMat.Clone(),
            Scale = scale,
            PadLeft = padLeft,
            PadTop = padTop,
            OriginalSize = new Size(originalWidth, originalHeight)
        };
    }

    /// <summary>
    ///     Unmaps a bounding box from processed image coordinates back to original image coordinates.
    /// </summary>
    /// <param name="rect">Rectangle in processed image coordinates.</param>
    /// <param name="resizeAndPadInfo">Resize and pad transformation information.</param>
    /// <returns>Rectangle in original image coordinates.</returns>
    private static Rectangle UnmapBoundingBox(Rectangle rect, ResizeAndPadInfo resizeAndPadInfo)
    {
        if (resizeAndPadInfo.Scale >= 1.0f)
            return rect; // No scaling was applied, only padding

        var x = RoundToIntAwayFromZero((rect.X - resizeAndPadInfo.PadLeft) / resizeAndPadInfo.Scale);
        var y = RoundToIntAwayFromZero((rect.Y - resizeAndPadInfo.PadTop) / resizeAndPadInfo.Scale);
        var w = RoundToIntAwayFromZero(rect.Width / resizeAndPadInfo.Scale);
        var h = RoundToIntAwayFromZero(rect.Height / resizeAndPadInfo.Scale);

        return new Rectangle(Math.Max(0, x), Math.Max(0, y), w, h);
    }

    /// <summary>
    ///     Unmaps a landmark point from processed image coordinates back to original image coordinates.
    /// </summary>
    /// <param name="point">Point in processed image coordinates.</param>
    /// <param name="resizeAndPadInfo">Resize and pad transformation information.</param>
    /// <returns>Point in original image coordinates, or <see langword="null" /> if outside bounds.</returns>
    private static Point? UnmapLandmark(Point point, ResizeAndPadInfo resizeAndPadInfo)
    {
        if (resizeAndPadInfo.Scale >= 1.0f)
            return point; // No scaling was applied, only padding

        var x = RoundToIntAwayFromZero((point.X - resizeAndPadInfo.PadLeft) / resizeAndPadInfo.Scale);
        var y = RoundToIntAwayFromZero((point.Y - resizeAndPadInfo.PadTop) / resizeAndPadInfo.Scale);

        // Check if point is within original image bounds
        if (x >= 0 && x < resizeAndPadInfo.OriginalSize.Width && y >= 0 && y < resizeAndPadInfo.OriginalSize.Height)
            return new Point(x, y);

        return null;
    }

    /// <summary>
    ///     Rounds a floating-point value to the nearest integer, rounding away from zero for midpoints.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The rounded integer value.</returns>
    private static int RoundToIntAwayFromZero(float value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    ///     Contains transformation information from resizing and padding operation for coordinate unmapping.
    /// </summary>
    private sealed class ResizeAndPadInfo
    {
        /// <summary>
        ///     Gets the processed <see cref="OpenCvSharp.Mat" /> instance.
        /// </summary>
        public required OpenCvSharp.Mat ProcessedMat { get; init; }

        /// <summary>
        ///     Gets the scale factor applied to the original image.
        /// </summary>
        public required float Scale { get; init; }

        /// <summary>
        ///     Gets the number of pixels padded on the left side.
        /// </summary>
        public required int PadLeft { get; init; }

        /// <summary>
        ///     Gets the number of pixels padded on the top side.
        /// </summary>
        public required int PadTop { get; init; }

        /// <summary>
        ///     Gets the original size of the image before resizing and padding.
        /// </summary>
        public required Size OriginalSize { get; init; }
    }
}