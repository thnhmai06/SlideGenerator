using System.Drawing;
using OpenCvSharp;
using SlideGenerator.Images.Models;
using Point = System.Drawing.Point;
using Size = OpenCvSharp.Size;

namespace SlideGenerator.Images.Entities.Detectors;

/// <summary>
///     Asynchronous wrapper for <see cref="FaceDetectorYN" />.
/// </summary>
public sealed class YuNet(FaceDetectorYN core, Size inputSize) : FaceDetector
{
    private readonly SemaphoreSlim _detectLock = new(1, 1);

    public override async Task<IReadOnlyList<Face>> DetectAsync(Mat mat)
    {
        var faces = new List<Face>();
        if (mat.Empty()) return faces;

        var padInfo = ResizeAndPadMat(mat);
        using var processedMat = padInfo.ProcessedMat;
        using var result = new Mat();
        
        await _detectLock.WaitAsync().ConfigureAwait(false);
        try
        {
            core.Detect(processedMat, result);
        }
        finally
        {
            _detectLock.Release();
        }

        if (result.Empty() || result.Rows == 0 || result.Cols < 15)
            return faces;

        var matBorder = new Rectangle(0, 0, mat.Width, mat.Height);

        for (var i = 0; i < result.Rows; i++)
        {
            var score = GetFloat(14);
            var rawBbox = new Rectangle(GetInt(0), GetInt(1), GetInt(2), GetInt(3));
            
            var mappedRect = UnmapBoundingBox(rawBbox, padInfo);
            var rect = Rectangle.Intersect(mappedRect, matBorder);
            
            if (rect.Width <= 0 || rect.Height <= 0) continue;

            faces.Add(new Face(
                rect, 
                score, 
                UnmapLandmark(GetPoint(4, 5), padInfo),   // Right Eye
                UnmapLandmark(GetPoint(6, 7), padInfo),   // Left Eye
                UnmapLandmark(GetPoint(8, 9), padInfo),   // Nose
                UnmapLandmark(GetPoint(10, 11), padInfo), // Right Mouth
                UnmapLandmark(GetPoint(12, 13), padInfo)  // Left Mouth
            ));
            continue;
            
            float GetFloat(int col) => result.At<float>(i, col);
            int GetInt(int col) => RoundToIntAwayFromZero(GetFloat(col));
            Point GetPoint(int colX, int colY) => new(GetInt(colX), GetInt(colY));
        }

        return faces;
    }

    public override void Dispose()
    {
        core.Dispose();
        _detectLock.Dispose();
    }

    private PaddingInfo ResizeAndPadMat(Mat mat)
    {
        var scaleX = (float)inputSize.Width / mat.Width;
        var scaleY = (float)inputSize.Height / mat.Height;
        var scale = Math.Min(1.0f, Math.Min(scaleX, scaleY));

        var newWidth = RoundToIntAwayFromZero(mat.Width * scale);
        var newHeight = RoundToIntAwayFromZero(mat.Height * scale);
        var padLeft = (inputSize.Width - newWidth) / 2;
        var padTop = (inputSize.Height - newHeight) / 2;

        var processedMat = new Mat(inputSize, mat.Type(), Scalar.Black);
        var roi = new Rect(padLeft, padTop, newWidth, newHeight);
        
        using var subMat = processedMat[roi];
        if (scale < 1.0f)
        {
            Cv2.Resize(mat, subMat, new Size(newWidth, newHeight));
        }
        else
        {
            mat.CopyTo(subMat);
        }

        return new PaddingInfo
        {
            ProcessedMat = processedMat,
            Scale = scale,
            PadLeft = padLeft,
            PadTop = padTop,
            OriginalSize = new Size(mat.Width, mat.Height)
        };
    }

    private static Rectangle UnmapBoundingBox(Rectangle rect, PaddingInfo info)
    {
        if (info.Scale >= 1.0f) return rect;

        var x = Math.Max(0, Unmap(rect.X, info.PadLeft));
        var y = Math.Max(0, Unmap(rect.Y, info.PadTop));
        
        return new Rectangle(x, y, UnmapLen(rect.Width), UnmapLen(rect.Height));

        int Unmap(int val, int pad) => RoundToIntAwayFromZero((val - pad) / info.Scale);

        int UnmapLen(int val) => RoundToIntAwayFromZero(val / info.Scale);
    }

    private static Point? UnmapLandmark(Point point, PaddingInfo info)
    {
        if (info.Scale >= 1.0f) return point;

        var x = RoundToIntAwayFromZero((point.X - info.PadLeft) / info.Scale);
        var y = RoundToIntAwayFromZero((point.Y - info.PadTop) / info.Scale);

        if (x >= 0 && x < info.OriginalSize.Width && y >= 0 && y < info.OriginalSize.Height)
            return new Point(x, y);

        return null;
    }

    private static int RoundToIntAwayFromZero(float value) 
        => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private sealed record PaddingInfo
    {
        public required Mat ProcessedMat { get; init; }
        public required float Scale { get; init; }
        public required int PadLeft { get; init; }
        public required int PadTop { get; init; }
        public required Size OriginalSize { get; init; }
    }
}