/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: FaceDetectorPoolTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using FluentAssertions;
using NSubstitute;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Domain.Models;
using SlideGenerator.Image.Infrastructure.Services;
using Xunit;

// ReSharper disable AccessToDisposedClosure

namespace SlideGenerator.Image.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="FaceDetectorPool" />, verifying concurrent detection respects
///     pool limits and that detector slots are correctly released after use or on exception.
/// </summary>
public sealed class FaceDetectorPoolTests
{
    #region DetectAsync — result forwarding

    /// <summary>
    ///     Verifies that <see cref="FaceDetectorPool.DetectAsync" /> returns the face list produced
    ///     by the underlying detector unchanged.
    /// </summary>
    [Fact]
    public async Task DetectAsync_Normal_ForwardsDetectorResult()
    {
        IReadOnlyList<Face> expected = [new(new Rectangle(10, 10, 50, 50), 0.9f)];
        var detector = Substitute.For<IFaceDetector>();
        detector.DetectAsync(Arg.Any<IImage>()).Returns(Task.FromResult(expected));

        using var pool = new FaceDetectorPool(() => detector, () => 5);

        var result = await pool.DetectAsync(CreateImage());

        result.Should().BeEquivalentTo(expected);
    }

    #endregion

    #region DetectAsync — exception safety

    /// <summary>
    ///     Verifies that <see cref="FaceDetectorPool.DetectAsync" /> releases the detector slot
    ///     even when the underlying detector throws, so further calls do not deadlock at limit=1.
    /// </summary>
    [Fact]
    public async Task DetectAsync_DetectorThrows_ReleasesSlot()
    {
        var calls = 0;
        var detector = Substitute.For<IFaceDetector>();
        detector.DetectAsync(Arg.Any<IImage>()).Returns(_ =>
            Interlocked.Increment(ref calls) == 1
                ? Task.FromException<IReadOnlyList<Face>>(new InvalidOperationException("simulated"))
                : Task.FromResult<IReadOnlyList<Face>>(Array.Empty<Face>()));

        using var pool = new FaceDetectorPool(() => detector, () => 1);
        var image = CreateImage();

        await FluentActions.Awaiting(() => pool.DetectAsync(image))
            .Should().ThrowAsync<InvalidOperationException>();

        // Slot was released — second call must complete, not deadlock
        var result = await pool.DetectAsync(image)
            .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
    }

    #endregion

    #region Helpers

    /// <summary>
    ///     Tracks peak concurrent detection calls across a pool run.
    /// </summary>
    private sealed class ConcurrencyTracker
    {
        public int Current;
        public int Max;
    }

    /// <summary>
    ///     Creates a mock <see cref="IImage" /> for use as detector input.
    /// </summary>
    private static IImage CreateImage()
    {
        var image = Substitute.For<IImage>();
        image.ToPng().Returns([]);
        return image;
    }

    /// <summary>
    ///     Creates a mock <see cref="IFaceDetector" /> that tracks concurrent calls and simulates
    ///     detection work by delaying for <paramref name="workMs" /> milliseconds.
    /// </summary>
    /// <param name="tracker">Shared tracker updated while detection is in progress.</param>
    /// <param name="workMs">How long each simulated detection takes.</param>
    private static IFaceDetector CreateTrackingDetector(ConcurrencyTracker tracker, int workMs = 80)
    {
        var detector = Substitute.For<IFaceDetector>();
        detector.DetectAsync(Arg.Any<IImage>()).Returns(_ => Task.Run(async () =>
        {
            var snapshot = Interlocked.Increment(ref tracker.Current);
            int prev;
            do
            {
                prev = tracker.Max;
                if (snapshot <= prev) break;
            } while (Interlocked.CompareExchange(ref tracker.Max, snapshot, prev) != prev);

            await Task.Delay(workMs, TestContext.Current.CancellationToken);
            Interlocked.Decrement(ref tracker.Current);
            return (IReadOnlyList<Face>)Array.Empty<Face>();
        }));
        return detector;
    }

    #endregion

    #region DetectAsync — concurrency

    /// <summary>
    ///     Verifies that concurrent calls to <see cref="FaceDetectorPool.DetectAsync" /> never
    ///     exceed the pool limit, measured by the peak number of detections in progress simultaneously.
    /// </summary>
    [Fact]
    public async Task DetectAsync_ConcurrentCalls_NeverExceedPoolLimit()
    {
        const int limit = 2;
        const int totalCalls = 8;
        var tracker = new ConcurrencyTracker();

        using var pool = new FaceDetectorPool(
            () => CreateTrackingDetector(tracker),
            () => limit);

        var image = CreateImage();
        await Task.WhenAll(Enumerable.Range(0, totalCalls).Select(_ => pool.DetectAsync(image)));

        tracker.Max.Should().BeLessThanOrEqualTo(limit);
    }

    /// <summary>
    ///     Verifies that when calls exceed the pool limit, all callers eventually complete
    ///     after the pool processes them in waves.
    /// </summary>
    [Fact]
    public async Task DetectAsync_CallsExceedLimit_AllEventuallyComplete()
    {
        const int limit = 3;
        const int totalCalls = 9;
        var tracker = new ConcurrencyTracker();

        using var pool = new FaceDetectorPool(
            () => CreateTrackingDetector(tracker, 50),
            () => limit);

        var image = CreateImage();
        var results = await Task.WhenAll(
            Enumerable.Range(0, totalCalls).Select(_ => pool.DetectAsync(image)));

        results.Should().HaveCount(totalCalls);
        tracker.Max.Should().BeLessThanOrEqualTo(limit);
    }

    #endregion
}