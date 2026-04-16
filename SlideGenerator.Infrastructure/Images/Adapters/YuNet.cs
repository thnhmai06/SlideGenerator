using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using SlideGenerator.Domain.Images.Abstractions;
using SlideGenerator.Domain.Images.Models;
using Point = System.Drawing.Point;
using Size = OpenCvSharp.Size;

namespace SlideGenerator.Infrastructure.Images.Adapters;

/// <summary>
///     Asynchronous wrapper for <see cref="FaceDetectorYN" />.
/// </summary>
/// <param name="inputSize">The path to the requested model</param>
/// <param name="configPath">The size of the input image that Mat will resize to in detection</param>
/// <param name="scoreThreshold">The path to the config file for compatibility, which is not requested for ONNX models</param>
/// <param name="nmsThreshold">The threshold to filter out bounding boxes of score smaller than the given value</param>
/// <param name="topK">The threshold to suppress bounding boxes of IoU bigger than the given value</param>
/// <param name="backendId">Keep top K b-boxes before NMS</param>
/// <param name="targetId">The id of backend</param>
/// <param name="targetId">The id of target device</param>
/// Reviewed by @thnhmai06 at 02/03/2026 11:41:42 GMT+7
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
    private FaceDetectorYN? _model;

    /// <summary>
    ///     Gets the semaphore used to coordinate access to lock detection operations.
    /// </summary>
    /// <remarks>
    ///     Use this semaphore to ensure that lock detection logic is executed in a thread-safe manner.
    ///     The semaphore is initialized with a single slot, allowing only one concurrent operation. This property is
    ///     immutable and set during object initialization.
    /// </remarks>
    public SemaphoreSlim DetectLock { private get; init; } = new(1, 1);

    public override bool IsModelAvailable => _model is { IsDisposed: false };

    public override async ValueTask DisposeAsync()
    {
        await DeInitAsync();
        DetectLock.Dispose();
    }

    public override Task<bool> InitAsync()
    {
        if (!IsModelAvailable)
            _model = FaceDetectorYN.Create(
                modelPath, configPath ?? string.Empty, inputSize,
                scoreThreshold, nmsThreshold, topK, backendId, targetId);

        return Task.FromResult(IsModelAvailable);
    }

    public override async Task<bool> DeInitAsync()
    {
        await DetectLock.WaitAsync().ConfigureAwait(false);
        if (IsModelAvailable)
        {
            _model?.Dispose();
            _model = null;
        }

        return !IsModelAvailable;
    }

    public override async Task<IReadOnlyList<Face>> DetectAsync(IMat mat)
    {
        return await DetectAsync(((Mat)mat).Core).ConfigureAwait(false);
    }

    /// <summary>
    ///     Detects faces from the provided mat using initialized YuNet model instance.
    /// </summary>
    /// <param name="mat">Input mat to run detection on.</param>
    /// <returns>All faces detected by the model without score filtering.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the model has not been initialized.</exception>
    public async Task<List<Face>> DetectAsync(OpenCvSharp.Mat mat)
    {
        if (!IsModelAvailable)
            throw new InvalidOperationException("The model is not initialized.");

        var faces = new List<Face>();
        if (mat.Empty()) return faces;

        // Resize and pad mat to match InputSize
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
    ///     Resizes and pads the input mat to match InputSize with black padding.
    ///     If input is smaller than InputSize, only adds black padding.
    ///     If input is larger, resizes proportionally while maintaining aspect ratio, then adds black padding.
    /// </summary>
    /// <param name="mat">Original input mat.</param>
    /// <returns>Resize and pad transformation information containing the processed mat and parameters for unmapping.</returns>
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

        // Create output mat with black background
        using var processedMat = new OpenCvSharp.Mat(new Size(targetWidth, targetHeight), mat.Type(), new Scalar(0, 0, 0));
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
    /// <returns>Point in original image coordinates, or null if outside bounds.</returns>
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
    ///     Contains transformation information from resizing and padding operation for coordinate unmapping.
    /// </summary>
    private sealed class ResizeAndPadInfo
    {
        public required OpenCvSharp.Mat ProcessedMat { get; init; }
        public required float Scale { get; init; }
        public required int PadLeft { get; init; }
        public required int PadTop { get; init; }
        public required Size OriginalSize { get; init; }
    }

    private static int RoundToIntAwayFromZero(float value)
        => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}