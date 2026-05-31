/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: FaceDetectorPool.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Domain.Models;

namespace SlideGenerator.Image.Infrastructure.Services;

/// <summary>
///     An <see cref="IFaceDetector" /> backed by a pool of <see cref="IFaceDetector"/> instances.
///     Acquires one detector per <see cref="DetectAsync" /> call and releases it on completion,
///     allowing concurrent face-detection up to the pool limit.
/// </summary>
public sealed class FaceDetectorPool(Func<IFaceDetector> factory, Func<uint> limitResolver)
    : Pool<IFaceDetector>(factory, limitResolver), IFaceDetector
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Face>> DetectAsync(IMat mat)
    {
        var detector = await AcquireAsync().ConfigureAwait(false);
        try
        {
            return await detector.DetectAsync(mat).ConfigureAwait(false);
        }
        finally
        {
            Release(detector);
        }
    }
}
