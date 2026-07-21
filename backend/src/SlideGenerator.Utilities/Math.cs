/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities
 * File: Math.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Utilities;

/// <summary>Pure math helpers for retry/backoff calculations.</summary>
public static class Math
{
    /// <summary>
    ///     Computes the truncated exponential backoff delay with jitter for the <paramref name="attempt" />-th
    ///     failed request (0-based): <c>min(2^attempt + random(0..1000ms), maxDelay)</c>. See
    ///     https://developers.google.com/workspace/drive/labels/limits#exponential.
    /// </summary>
    /// <param name="attempt">0-based count of failed attempts so far.</param>
    /// <param name="maxDelay">Upper bound the computed delay will never exceed.</param>
    public static TimeSpan ComputeBackoffDelay(int attempt, TimeSpan maxDelay)
    {
        var jitterMs = Random.Shared.Next(0, 1001);
        var backoff = TimeSpan.FromSeconds(System.Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(jitterMs);
        return backoff < maxDelay ? backoff : maxDelay;
    }
}
