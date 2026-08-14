/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: YuNetPool.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Image.Loading;
using SlideGenerator.Utilities;

namespace SlideGenerator.Image.FaceDetection;

/// <summary>
///     An <see cref="IFaceDetector" /> backed by a pool of <see cref="IFaceDetector" /> instances.
///     Acquires one detector per <see cref="DetectAsync" /> call and releases it on completion,
///     allowing concurrent face-detection up to the pool limit.
/// </summary>
/// <param name="limitResolver">Returns the current maximum number of pooled detectors.</param>
/// <param name="detectorFactory">
///     Creates a new pooled detector instance. Defaults to <see cref="YuNet" />; overridable for testing.
/// </param>
public sealed class YuNetPool(Func<uint> limitResolver, Func<IFaceDetector>? detectorFactory = null)
    : Pool<IFaceDetector>(detectorFactory ?? (() => new YuNet()), limitResolver), IFaceDetector
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Face>> DetectAsync(IImage image)
    {
        var detector = await AcquireAsync().ConfigureAwait(false);
        try
        {
            return await detector.DetectAsync(image).ConfigureAwait(false);
        }
        finally
        {
            Release(detector);
        }
    }
}