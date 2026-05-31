/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: YuNet.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using OpenCvSharp;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Domain;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Domain.Models;
using Point = System.Drawing.Point;
using Size = OpenCvSharp.Size;

namespace SlideGenerator.Image.Infrastructure.Adapters;

/// <summary>
///     Asynchronous wrapper for <see cref="FaceDetectorYN" />.
/// </summary>
internal sealed class YuNet : IFaceDetector
{
    private static readonly string ModelPath =
        Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Binary", "YuNet.onnx");

    private readonly FaceDetectorYN _core;
    private readonly Size _inputSize;
    private readonly Lock _detectLock = new();

    /// <summary>
    ///     Initializes a new <see cref="YuNet" /> instance, loading the ONNX model from disk.
    /// </summary>
    public YuNet()
    {
        _inputSize = Rules.FaceInputSize.ToOpenCv();
        _core = FaceDetectorYN.Create(ModelPath, string.Empty, _inputSize, Rules.FaceConfidence);
    }

    public Task<IReadOnlyList<Face>> DetectAsync(IMat imat)
    {
        var faces = new List<Face>();
        if (imat.Empty) return Task.FromResult<IReadOnlyList<Face>>(faces);

        if (imat is not OpenCvMat adapter)
            throw new ArgumentException($"IMat must be an instance of {nameof(OpenCvMat)}.", nameof(imat));

        var mat = adapter.Mat;

        var padInfo = ResizeAndPadMat(mat);
        using var processedMat = padInfo.ProcessedMat;
        using var result = new Mat();

        lock (_detectLock)
        {
            _core.Detect(processedMat, result);
        }

        if (result.Empty() || result.Rows == 0 || result.Cols < 15)
            return Task.FromResult<IReadOnlyList<Face>>(faces);

        var matBorder = new Rectangle(0, 0, mat.Width, mat.Height);

        var rows = result.Rows;
        for (var i = 0; i < rows; i++)
        {
            var score = GetFloat(14);
            var rawBbox = new Rectangle(GetInt(0), GetInt(1), GetInt(2), GetInt(3));

            var mappedRect = UnmapBoundingBox(rawBbox, padInfo);
            var rect = Rectangle.Intersect(mappedRect, matBorder);

            if (rect.Width <= 0 || rect.Height <= 0) continue;

            faces.Add(new Face(
                rect,
                score,
                UnmapLandmark(GetPoint(4, 5), padInfo), // Right Eye
                UnmapLandmark(GetPoint(6, 7), padInfo), // Left Eye
                UnmapLandmark(GetPoint(8, 9), padInfo), // Nose
                UnmapLandmark(GetPoint(10, 11), padInfo), // Right Mouth
                UnmapLandmark(GetPoint(12, 13), padInfo) // Left Mouth
            ));
            continue;

            float GetFloat(int col) => result.At<float>(i, col);
            int GetInt(int col) => RoundToIntAwayFromZero(GetFloat(col));
            Point GetPoint(int colX, int colY) => new(GetInt(colX), GetInt(colY));
        }

        return Task.FromResult<IReadOnlyList<Face>>(faces);
    }

    public void Dispose()
    {
        _core.Dispose();
    }

    private PaddingInfo ResizeAndPadMat(Mat mat)
    {
        var scaleX = (float)_inputSize.Width / mat.Width;
        var scaleY = (float)_inputSize.Height / mat.Height;
        var scale = Math.Min(1.0f, Math.Min(scaleX, scaleY));

        var newWidth = RoundToIntAwayFromZero(mat.Width * scale);
        var newHeight = RoundToIntAwayFromZero(mat.Height * scale);
        var padLeft = (_inputSize.Width - newWidth) / 2;
        var padTop = (_inputSize.Height - newHeight) / 2;

        var processedMat = new Mat(_inputSize, mat.Type(), Scalar.Black);
        var roi = new Rect(padLeft, padTop, newWidth, newHeight);

        using var subMat = processedMat[roi];
        if (scale < 1.0f)
            Cv2.Resize(mat, subMat, new Size(newWidth, newHeight));
        else
            mat.CopyTo(subMat);

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

        int Unmap(int val, int pad)
        {
            return RoundToIntAwayFromZero((val - pad) / info.Scale);
        }

        int UnmapLen(int val)
        {
            return RoundToIntAwayFromZero(val / info.Scale);
        }
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
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private sealed record PaddingInfo
    {
        public required Mat ProcessedMat { get; init; }
        public required float Scale { get; init; }
        public required int PadLeft { get; init; }
        public required int PadTop { get; init; }
        public required Size OriginalSize { get; init; }
    }
}
