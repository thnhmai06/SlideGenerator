/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: YuNetPoolTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

// ReSharper disable AccessToDisposedClosure
using System.Drawing;
using SlideGenerator.Image.FaceDetection;
using FluentAssertions;
using SlideGenerator.Image.Loading;
using NSubstitute;
using Xunit;

namespace SlideGenerator.Image.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="YuNetPool" />, verifying concurrent detection respects
///     pool limits and that detector slots are correctly released after use or on exception.
/// </summary>
public sealed class YuNetPoolTests
{
    #region DetectAsync — result forwarding

    /// <summary>
    ///     Verifies that <see cref="YuNetPool.DetectAsync" /> returns the face list produced
    ///     by the underlying detector unchanged.
    /// </summary>
    [Fact]
    public async Task DetectAsync_Normal_ForwardsDetectorResult()
    {
        IReadOnlyList<Face> expected = [new(new Rectangle(10, 10, 50, 50), 0.9f)];
        var detector = Substitute.For<IFaceDetector>();
        detector.DetectAsync(Arg.Any<IImage>()).Returns(Task.FromResult(expected));

        using var pool = new YuNetPool(() => 5, () => detector);

        var result = await pool.DetectAsync(CreateImage());

        result.Should().BeEquivalentTo(expected);
    }

    #endregion

    #region DetectAsync — exception safety

    /// <summary>
    ///     Verifies that <see cref="YuNetPool.DetectAsync" /> releases the detector slot
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
                : Task.FromResult<IReadOnlyList<Face>>([]));

        using var pool = new YuNetPool(() => 1, () => detector);
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
    ///     An <see cref="IFaceDetector" /> that records peak concurrent <see cref="DetectAsync" />
    ///     invocations on a shared <see cref="ConcurrencyTracker" /> and holds each call open briefly so
    ///     overlapping calls actually overlap, giving the pool's concurrency limit something to enforce.
    /// </summary>
    private sealed class TrackingDetector(ConcurrencyTracker tracker) : IFaceDetector
    {
        public void Dispose()
        {
        }

        public async Task<IReadOnlyList<Face>> DetectAsync(IImage image)
        {
            var current = Interlocked.Increment(ref tracker.Current);
            UpdateMax(current);
            try
            {
                await Task.Delay(25, TestContext.Current.CancellationToken).ConfigureAwait(false);
                return [];
            }
            finally
            {
                Interlocked.Decrement(ref tracker.Current);
            }
        }

        private void UpdateMax(int current)
        {
            while (true)
            {
                var observed = tracker.Max;
                if (current <= observed) return;
                if (Interlocked.CompareExchange(ref tracker.Max, current, observed) == observed) return;
            }
        }
    }

    /// <summary>
    ///     Creates a mock <see cref="IImage" /> for use as a detector input.
    /// </summary>
    private static IImage CreateImage()
    {
        var image = Substitute.For<IImage>();
        image.ToPng().Returns([]);
        return image;
    }

    #endregion

    #region DetectAsync — concurrency

    /// <summary>
    ///     Verifies that concurrent calls to <see cref="YuNetPool.DetectAsync" /> never
    ///     exceed the pool limit, measured by the peak number of detections in progress simultaneously.
    /// </summary>
    [Fact]
    public async Task DetectAsync_ConcurrentCalls_NeverExceedPoolLimit()
    {
        const int limit = 2;
        const int totalCalls = 8;
        var tracker = new ConcurrencyTracker();

        using var pool = new YuNetPool(() => limit, () => new TrackingDetector(tracker));

        var image = CreateImage();
        await Task.WhenAll(Enumerable.Range(0, totalCalls).Select(_ => pool.DetectAsync(image)));

        tracker.Max.Should().BeLessThanOrEqualTo(limit);
        tracker.Max.Should().BeGreaterThan(0,
            "the calls must overlap to actually exercise the pool limit — otherwise this test measures nothing");
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

        using var pool = new YuNetPool(() => limit, () => new TrackingDetector(tracker));

        var image = CreateImage();
        var results = await Task.WhenAll(
            Enumerable.Range(0, totalCalls).Select(_ => pool.DetectAsync(image)));

        results.Should().HaveCount(totalCalls);
        tracker.Max.Should().BeLessThanOrEqualTo(limit);
        tracker.Max.Should().BeGreaterThan(0,
            "the calls must overlap to actually exercise the pool limit — otherwise this test measures nothing");
    }

    #endregion
}